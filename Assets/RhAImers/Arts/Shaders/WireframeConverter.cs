using UnityEngine;

namespace RhAImers.Arts
{
    /// <summary>
    /// Attach this script to a GameObject to convert its meshes (or children's meshes) to wireframe at runtime.
    /// This is a fallback solution if Geometry Shaders are not supported on the target platform (like some Macs or WebGL).
    /// </summary>
    public class WireframeConverter : MonoBehaviour
    {
        [Tooltip("The material to use for the wireframe lines. Use an Unlit material with HDR color for a neon effect.")]
        public Material wireframeMaterial;

        [Tooltip("Apply wireframe conversion on Start?")]
        public bool applyOnStart = true;

        [Tooltip("ワイヤーの密度（0.0〜1.0）。メッシュが細かすぎて全身が1色に塗りつぶされる場合は、この数値を0.1〜0.3などに下げて間引く。")]
        [Range(0.01f, 1f)]
        public float lineDensity = 0.3f;

        void Start()
        {
            if (applyOnStart)
            {
                ApplyWireframe();
            }
        }

        [ContextMenu("Apply Wireframe Converter")]
        public void ApplyWireframe()
        {
            // Convert SkinnedMeshRenderers
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (smr.sharedMesh != null && smr.sharedMesh.GetTopology(0) != MeshTopology.Lines)
                {
                    smr.sharedMesh = ConvertToWireframe(smr.sharedMesh);
                    if (wireframeMaterial != null)
                    {
                        Material[] newMats = new Material[smr.sharedMaterials.Length];
                        for (int i = 0; i < newMats.Length; i++) newMats[i] = wireframeMaterial;
                        smr.sharedMaterials = newMats;
                    }
                }
            }

            // Convert MeshFilters (for MeshRenderers)
            foreach (var mf in GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh != null && mf.sharedMesh.GetTopology(0) != MeshTopology.Lines)
                {
                    mf.sharedMesh = ConvertToWireframe(mf.sharedMesh);
                    var mr = mf.GetComponent<MeshRenderer>();
                    if (mr != null && wireframeMaterial != null)
                    {
                        Material[] newMats = new Material[mr.sharedMaterials.Length];
                        for (int i = 0; i < newMats.Length; i++) newMats[i] = wireframeMaterial;
                        mr.sharedMaterials = newMats;
                    }
                }
            }
        }

        private Mesh ConvertToWireframe(Mesh originalMesh)
        {
            Mesh wireframeMesh = Instantiate(originalMesh);
            wireframeMesh.name = originalMesh.name + "_WireframeLines";
            
            // Re-seed to keep it consistent if needed, or leave it random for glitchy effects
            Random.InitState(42); 
            
            for (int submesh = 0; submesh < wireframeMesh.subMeshCount; submesh++)
            {
                int[] triangles = wireframeMesh.GetTriangles(submesh);
                System.Collections.Generic.List<int> linesList = new System.Collections.Generic.List<int>();
                
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    if (lineDensity >= 1f || Random.value <= lineDensity)
                    {
                        linesList.Add(triangles[i]);
                        linesList.Add(triangles[i + 1]);
                    }
                    if (lineDensity >= 1f || Random.value <= lineDensity)
                    {
                        linesList.Add(triangles[i + 1]);
                        linesList.Add(triangles[i + 2]);
                    }
                    if (lineDensity >= 1f || Random.value <= lineDensity)
                    {
                        linesList.Add(triangles[i + 2]);
                        linesList.Add(triangles[i]);
                    }
                }
                
                wireframeMesh.SetIndices(linesList.ToArray(), MeshTopology.Lines, submesh);
            }
            
            return wireframeMesh;
        }
    }
}
