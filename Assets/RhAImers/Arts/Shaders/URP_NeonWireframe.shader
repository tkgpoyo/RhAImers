Shader "Custom/URP_NeonWireframe"
{
    Properties
    {
        [HDR] _Color ("Wireframe Color", Color) = (0, 1, 1, 1)
        _Thickness ("Wireframe Thickness", Range(0.0, 1.0)) = 0.05
        _Glow ("Glow Multiplier", Range(1, 10)) = 2.0
        _BaseColor ("Base Color (Optional)", Color) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma require geometry
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct GeometryOutput
            {
                float4 positionCS   : SV_POSITION;
                float3 barycentric  : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _BaseColor;
                float _Thickness;
                float _Glow;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            [maxvertexcount(3)]
            void geom(triangle Varyings input[3], inout TriangleStream<GeometryOutput> triStream)
            {
                GeometryOutput o = (GeometryOutput)0;
                
                o.positionCS = input[0].positionCS;
                o.barycentric = float3(1, 0, 0);
                triStream.Append(o);

                o.positionCS = input[1].positionCS;
                o.barycentric = float3(0, 1, 0);
                triStream.Append(o);

                o.positionCS = input[2].positionCS;
                o.barycentric = float3(0, 0, 1);
                triStream.Append(o);
            }

            half4 frag(GeometryOutput input) : SV_Target
            {
                // Smooth line rendering using ddx/ddy (fwidth)
                float3 barys = input.barycentric;
                float3 deltas = fwidth(barys);
                
                // Thickness control
                float3 smoothing = deltas * 1.5;
                float3 thickness = deltas * (_Thickness * 50.0);
                
                barys = smoothstep(thickness, thickness + smoothing, barys);
                float minBary = min(barys.x, min(barys.y, barys.z));
                
                float wireframeAlpha = 1.0 - minBary;
                
                // Combine wireframe and base color
                half4 wireColor = half4((_Color * _Glow).rgb, _Color.a * wireframeAlpha);
                
                // Alpha blend with base color if provided
                half4 finalColor = lerp(_BaseColor, half4(wireColor.rgb, 1.0), wireframeAlpha);
                finalColor.a = max(_BaseColor.a, wireframeAlpha);
                
                if (finalColor.a < 0.01) discard;

                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/InternalErrorShader"
}
