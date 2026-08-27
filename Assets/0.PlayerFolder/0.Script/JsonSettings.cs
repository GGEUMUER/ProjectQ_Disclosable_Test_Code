using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

public static class JsonSettings
{
    public static readonly JsonSerializerSettings CamelCaseSettings = new JsonSerializerSettings
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Formatting = Formatting.None
    };
}
