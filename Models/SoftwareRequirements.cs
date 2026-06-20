using Newtonsoft.Json;

namespace CanIRunIt.Models
{
    public class SoftwareRequirements
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("category")]
        public string Category { get; set; } = string.Empty;

        [JsonProperty("minimum")]
        public Requirements Minimum { get; set; } = new Requirements();

        [JsonProperty("recommended")]
        public Requirements Recommended { get; set; } = new Requirements();
    }

    public class Requirements
    {
        [JsonProperty("cpu")]
        public string CPU { get; set; } = string.Empty;

        [JsonProperty("ram")]
        public string RAM { get; set; } = string.Empty;

        [JsonProperty("gpu")]
        public string GPU { get; set; } = string.Empty;

        [JsonProperty("storage")]
        public string Storage { get; set; } = string.Empty;

        [JsonProperty("os")]
        public string OS { get; set; } = string.Empty;
    }
}
