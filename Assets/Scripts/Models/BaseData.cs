using Newtonsoft.Json;

namespace Assets.Scripts.Models
{
    using J = JsonPropertyAttribute;

    public abstract class BaseData
    {
        [J("_version")] public int Version { get; set; } = 1;
    }
}
