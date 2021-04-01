using Newtonsoft.Json;
using System;

namespace PepperDash.Plugin.Display.SamsungMdc
{
	public class SamsungMDCDisplayPropertiesConfig
	{
		[JsonProperty("id")]
		public string Id { get; set; }

        [JsonProperty("volumeUpperLimit")]
        public int volumeUpperLimit { get; set; }

        [JsonProperty("volumeLowerLimit")]
        public int volumeLowerLimit { get; set; }

        [JsonProperty("pollIntervalMs")]
        public long pollIntervalMs { get; set; }

        [JsonProperty("coolingTimeMs")]
        public uint coolingTimeMs { get; set; }

        [JsonProperty("warmingTimeMs")]
        public uint warmingTimeMs { get; set; }

		[JsonProperty("showVolumeControls")]
		public bool showVolumeControls { get; set; }
	}
}