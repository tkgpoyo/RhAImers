using UnityEngine;

[CreateAssetMenu(fileName = "ApiKeyConfig", menuName = "Config/ApiKeyConfig")]
public class ApiKeyConfig : ScriptableObject
{
    [SerializeField]
    private string _apiKey;
    public string ApiKey => _apiKey;

    private static ApiKeyConfig _instance;
    public static ApiKeyConfig Instance => _instance ??= Resources.Load<ApiKeyConfig>("Configs/ApiKeyConfig");
}
