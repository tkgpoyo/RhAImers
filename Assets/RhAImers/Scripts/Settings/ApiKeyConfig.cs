using UnityEngine;

[CreateAssetMenu(fileName = "ApiKeyConfig", menuName = "Config/ApiKeyConfig")]
public class ApiKeyConfig : ScriptableObject
{
    [SerializeField]
    private string _apiKey;

    public string ApiKey => _apiKey;
}
