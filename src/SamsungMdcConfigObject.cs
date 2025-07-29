using System.Collections.Generic;
using Newtonsoft.Json;
using PepperDash.Core;

namespace PepperDashPluginSamsungMdcDisplay
{
    /// <summary>
    /// Configuration class for Samsung MDC display device properties. Contains settings for device ID,
    /// volume limits, polling intervals, temperature monitoring, custom inputs, and other device-specific parameters.
    /// </summary>
    public class SamsungMdcDisplayPropertiesConfig
    {
        /// <summary>
        /// Gets or sets the unique device identifier for MDC communication. 
        /// Should be a hexadecimal string value (e.g., "01", "0A", "FF").
        /// </summary>
        /// <value>A hexadecimal string representing the device ID for MDC protocol communication.</value>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the upper limit for volume control. Volume commands will be scaled 
        /// between the lower and upper limits.
        /// </summary>
        /// <value>The maximum volume level (0-100 range).</value>
        [JsonProperty("volumeUpperLimit")]
        public int VolumeUpperLimit { get; set; }

        /// <summary>
        /// Gets or sets the lower limit for volume control. Volume commands will be scaled 
        /// between the lower and upper limits.
        /// </summary>
        /// <value>The minimum volume level (0-100 range).</value>
        [JsonProperty("volumeLowerLimit")]
        public int VolumeLowerLimit { get; set; }

        /// <summary>
        /// Gets or sets the polling interval in milliseconds for status updates from the display.
        /// Determines how frequently the display is polled for status information.
        /// </summary>
        /// <value>The polling interval in milliseconds.</value>
        [JsonProperty("pollIntervalMs")]
        public long PollIntervalMs { get; set; }

        /// <summary>
        /// Gets or sets the cooling time in milliseconds. This is the time the display 
        /// requires to cool down after being powered off.
        /// </summary>
        /// <value>The cooling time duration in milliseconds.</value>
        [JsonProperty("coolingTimeMs")]
        public uint CoolingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the warming time in milliseconds. This is the time the display 
        /// requires to warm up after being powered on.
        /// </summary>
        /// <value>The warming time duration in milliseconds.</value>
        [JsonProperty("warmingTimeMs")]
        public uint WarmingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether volume controls should be visible in the user interface.
        /// </summary>
        /// <value>True if volume controls should be shown; otherwise, false.</value>
        [JsonProperty("showVolumeControls")]
        public bool ShowVolumeControls { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether LED temperature monitoring should be enabled.
        /// When enabled, the display will be polled for LED temperature information.
        /// </summary>
        /// <value>True if LED temperature polling is enabled; otherwise, false.</value>
        [JsonProperty("pollLedTemps")]
        public bool PollLedTemps { get; set; }

        /// <summary>
        /// Gets or sets the collection of friendly names for input ports. These provide 
        /// human-readable names for display inputs instead of technical identifiers.
        /// </summary>
        /// <value>A list of friendly name mappings for input ports.</value>
        [JsonProperty("friendlyNames")]
        public List<FriendlyName> FriendlyNames { get; set; }

        /// <summary>
        /// Gets or sets the collection of custom input definitions. These allow for non-standard 
        /// input types that may not be covered by the default input set.
        /// </summary>
        /// <value>A list of custom input configurations.</value>
        [JsonProperty("customInputs")]
        public List<CustomInput> CustomInputs { get; set; }

        /// <summary>
        /// Gets or sets the collection of active inputs available on this display. 
        /// This filters the available inputs to only those that should be exposed to users.
        /// </summary>
        /// <value>A list of active input configurations.</value>
        [JsonProperty("activeInputs")]
        public List<ActiveInputs> ActiveInputs { get; set; } 

        /// <summary>
        /// Initializes a new instance of the SamsungMdcDisplayPropertiesConfig class with default values.
        /// All list properties are initialized to empty lists.
        /// </summary>
        public SamsungMdcDisplayPropertiesConfig()
        {
            FriendlyNames = new List<FriendlyName>();
            CustomInputs = new List<CustomInput>();
            ActiveInputs = new List<ActiveInputs>();
        }
    }

    /// <summary>
    /// Represents a friendly name mapping for an input port, providing a human-readable 
    /// name for technical input identifiers.
    /// </summary>
    public class FriendlyName
    {
        /// <summary>
        /// Gets or sets the technical key identifier for the input port (e.g., "hdmi1", "displayPort1").
        /// </summary>
        /// <value>The input key identifier used internally by the system.</value>
        [JsonProperty("inputKey")]
        public string InputKey { get; set; }

        /// <summary>
        /// Gets or sets the friendly, human-readable name for the input port (e.g., "Laptop", "Apple TV").
        /// </summary>
        /// <value>The user-friendly name to display for this input.</value>
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Represents a custom input configuration for inputs that are not part of the standard input set.
    /// Allows for defining custom MDC commands and connection types.
    /// </summary>
    public class CustomInput
    {
        /// <summary>
        /// Gets or sets the hexadecimal command value to send for this custom input (e.g., "0x35").
        /// </summary>
        /// <value>A hexadecimal string representing the MDC command for this input.</value>
        [JsonProperty("inputCommand")]
        public string InputCommand { get; set; }
        
        /// <summary>
        /// Gets or sets the type of physical connection for this input (e.g., "Hdmi", "DisplayPort", "Dvi").
        /// </summary>
        /// <value>The connection type as a string that maps to eRoutingPortConnectionType enumeration.</value>
        [JsonProperty("inputConnector")]
        public string InputConnector { get; set; }
        
        /// <summary>
        /// Gets or sets the unique identifier for this custom input used for routing and selection.
        /// </summary>
        /// <value>A unique string identifier for this custom input.</value>
        [JsonProperty("inputIdentifier")]
        public string InputIdentifier { get; set; }
    }

    /// <summary>
    /// Represents an active input configuration that defines which inputs should be available 
    /// to users. Implements IKeyName interface for consistent key-name pairing.
    /// </summary>
    public class ActiveInputs : IKeyName
    {
        /// <summary>
        /// Gets or sets the unique key identifier for this active input (e.g., "hdmi1", "displayPort1").
        /// </summary>
        /// <value>The key identifier used to reference this input.</value>
        [JsonProperty("key")]
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the display name for this active input as it should appear to users.
        /// </summary>
        /// <value>The user-visible name for this input.</value>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Initializes a new instance of the ActiveInputs class with empty key and name values.
        /// </summary>
        public ActiveInputs()
        {
            Key = string.Empty;
            Name = string.Empty;
        }
    }
        

}