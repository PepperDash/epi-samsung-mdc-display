// Also compiled into the tests, which enable nullable reference types; the plugin doesn't
#nullable disable
using Newtonsoft.Json;

namespace PepperDashPluginSamsungMdcDisplay
{
    /// <summary>
    /// Starting state and behaviour of the mock panel, from the optional "mock" block in properties
    /// </summary>
    /// <remarks>
    /// No Crestron or PepperDash types, so the tests can compile it alongside the responder.
    /// </remarks>
    public class MockSamsungMdcOptions
    {
        /// <summary>
        /// Creates the defaults: panel off, HDMI 1, volume 20, unmuted, 50 ms replies
        /// </summary>
        public MockSamsungMdcOptions()
        {
            Input = "hdmi1";
            Volume = 20;
            TemperatureC = 40;
            SerialNumber = "MOCK0SAMSUNG000";
            SoftwareVersion = "MOCKPROJECT0MOCK-1000.0";
            IpAddress = "192.168.0.10";
            ResponseDelayMs = 50;
            NakWhileOff = true;
        }

        /// <summary>
        /// Delay before each reply frame, in ms. The driver doesn't time out replies, but keep it well
        /// under the poll interval.
        /// </summary>
        [JsonProperty("responseDelayMs")]
        public int ResponseDelayMs { get; set; }

        /// <summary>
        /// The panel's own MDC ID in hex ("00"-"FD"). Defaults to the device's "id" property so the
        /// mock answers; set it to something else to see what the driver does when no display answers.
        /// </summary>
        [JsonProperty("displayId")]
        public string DisplayId { get; set; }

        /// <summary>
        /// Whether the panel starts powered on
        /// </summary>
        [JsonProperty("powerOn")]
        public bool PowerOn { get; set; }

        /// <summary>
        /// Starting input: a name (hdmi1-4, displayPort1-3, dvi1, magicInfo, ...) or a hex code ("0x21", "21")
        /// </summary>
        [JsonProperty("input")]
        public string Input { get; set; }

        /// <summary>
        /// Starting volume, 0-100
        /// </summary>
        [JsonProperty("volume")]
        public int Volume { get; set; }

        /// <summary>
        /// Whether the panel starts muted
        /// </summary>
        [JsonProperty("muted")]
        public bool Muted { get; set; }

        /// <summary>
        /// Temperature reported by LED monitoring (0xD0 sub 0x84), in Celsius
        /// </summary>
        [JsonProperty("temperatureC")]
        public int TemperatureC { get; set; }

        /// <summary>
        /// Serial number reported by 0x0B (up to 15 characters)
        /// </summary>
        [JsonProperty("serialNumber")]
        public string SerialNumber { get; set; }

        /// <summary>
        /// Version string reported by 0x0E: 12 characters of project info, then the software version
        /// </summary>
        [JsonProperty("softwareVersion")]
        public string SoftwareVersion { get; set; }

        /// <summary>
        /// IP address reported by 0x1B sub 0x82
        /// </summary>
        [JsonProperty("ipAddress")]
        public string IpAddress { get; set; }

        /// <summary>
        /// While off, answer only status (0x00) and power (0x11) and NAK everything else. The spec
        /// doesn't say what a panel in standby answers; set false to answer everything while off.
        /// </summary>
        [JsonProperty("nakWhileOff")]
        public bool NakWhileOff { get; set; }

        /// <summary>
        /// Not spec behaviour: echo each command back ahead of its reply, in the same receive event.
        /// Reproduces the field condition fixed by PR #51 (short echoed frames).
        /// </summary>
        [JsonProperty("echoCommands")]
        public bool EchoCommands { get; set; }
    }
}
