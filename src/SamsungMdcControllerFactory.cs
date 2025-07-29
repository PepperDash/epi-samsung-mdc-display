using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace PepperDashPluginSamsungMdcDisplay
{
    /// <summary>
    /// Factory class responsible for creating instances of SamsungMdcDisplayController devices.
    /// Implements the EssentialsPluginDeviceFactory pattern for integration with the PepperDash Essentials framework.
    /// </summary>
    public class SamsungMdcControllerFactory:EssentialsPluginDeviceFactory<SamsungMdcDisplayController>
    {
        /// <summary>
        /// Initializes a new instance of the SamsungMdcControllerFactory class.
        /// Sets the minimum required Essentials framework version and registers the supported device type names.
        /// </summary>
        public SamsungMdcControllerFactory()
        {
            MinimumEssentialsFrameworkVersion = "2.4.7";

            TypeNames = new List<string> {"samsungMdcPlugin"};
        }

        #region Overrides of EssentialsDeviceFactory<SamsungMdcDisplayController>

        /// <summary>
        /// Creates a new SamsungMdcDisplayController device instance based on the provided device configuration.
        /// Establishes the communication interface and deserializes the device-specific configuration.
        /// </summary>
        /// <param name="dc">The device configuration containing connection details and device-specific properties.</param>
        /// <returns>A configured SamsungMdcDisplayController instance, or null if creation fails.</returns>
        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            var comms = CommFactory.CreateCommForDevice(dc);

            if (comms == null)
            {
                Debug.Console(0, Debug.ErrorLogLevel.Error, "Unable to create comms for device {0}", dc.Key);
                return null;
            }

            var config = dc.Properties.ToObject<SamsungMdcDisplayPropertiesConfig>();

            if (config != null)
            {
                return new SamsungMdcDisplayController(dc.Key, dc.Name, config, comms);
            }

            Debug.Console(0, Debug.ErrorLogLevel.Error, "Unable to deserialize config for device {0}", dc.Key);
            return null;
        }

        #endregion
    }
}