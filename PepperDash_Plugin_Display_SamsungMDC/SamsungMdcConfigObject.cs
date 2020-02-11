using Newtonsoft.Json;
using System;

namespace PepperDash.Plugin.Display.SamsungMdc
{
	public class SamsungMDCDisplayPropertiesConfig
	{
		[JsonProperty("id")]
		public string Id { get; set; }
	}
}