using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace PepperDashPluginSamsungMdcDisplay
{
    /// <summary>
    /// Builds the real <see cref="SamsungMdcDisplayController"/> on an in-process mock panel, so a room can be
    /// bench-tested without a display. A real config switches to the mock by changing only "type";
    /// any "control" block is ignored.
    /// </summary>
    public class MockSamsungMdcFactory : EssentialsPluginDeviceFactory<SamsungMdcDisplayController>
    {
        /// <summary>
        /// Registers the "mockSamsungMdc" type
        /// </summary>
        public MockSamsungMdcFactory()
        {
            TypeNames = new List<string> { "mockSamsungMdc" };

            // Same minimum as the real factory (SamsungMdcControllerFactory)
            MinimumEssentialsFrameworkVersion = "3.0.0";
        }

        /// <inheritdoc />
        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            var properties = dc.Properties ?? new JObject();
            var config = properties.ToObject<SamsungMdcDisplayPropertiesConfig>() ?? new SamsungMdcDisplayPropertiesConfig();

            var mockToken = properties["mock"];
            var options = mockToken != null && mockToken.Type == JTokenType.Object
                ? mockToken.ToObject<MockSamsungMdcOptions>() ?? new MockSamsungMdcOptions()
                : new MockSamsungMdcOptions();

            // The driver sends Convert.ToByte(id, 16), or 0x01 when "id" is missing; by default the panel
            // takes the same ID so it answers
            var displayId = ParseId(options.DisplayId, ParseId(config.Id, (byte)0x01));

            var responder = new MockSamsungMdcResponder(displayId, options);
            var comms = new MockSamsungMdcCommunication(dc.Key + "-mockComm", responder, options.ResponseDelayMs, options.EchoCommands);

            MockSamsungMdcConsole.Register(dc.Key, responder);
            Debug.LogInformation("Building mock Samsung MDC display '{0}' (no hardware, ID 0x{1:X2}). Console: mocksamsung {0} status",
                dc.Key, displayId);

            // Same constructor the real factory calls, with the fake in place of CommFactory's comms
            return new SamsungMdcDisplayController(dc.Key, dc.Name, config, comms);
        }

        // Same conversion as the driver, so "0x02" and "02" both give 0x02
        private static byte ParseId(string id, byte fallback)
        {
            if (string.IsNullOrEmpty(id))
                return fallback;

            try
            {
                return Convert.ToByte(id, 16);
            }
            catch (Exception e) when (e is FormatException || e is OverflowException || e is ArgumentException)
            {
                return fallback;
            }
        }
    }
}
