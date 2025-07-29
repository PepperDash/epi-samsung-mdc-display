using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;

namespace PepperDashPluginSamsungMdcDisplay
{
    /// <summary>
    /// Join map class for Samsung MDC display controller, extending the base DisplayControllerJoinMap
    /// with Samsung-specific joins for LED temperature monitoring, status feedback, and volume control visibility.
    /// Defines the mapping between SIMPL bridge joins and Samsung display functionality.
    /// </summary>
    public class SamsungDisplayControllerJoinMap : DisplayControllerJoinMap
    {
        /// <summary>
        /// Gets the analog join configuration for LED temperature feedback in Celsius.
        /// Provides the current LED temperature reading from the display's monitoring system.
        /// </summary>
        /// <value>Join data for LED temperature in Celsius (Join 21).</value>
        [JoinName("LedTemperatureCelsius")]
        public JoinDataComplete LedTemperatureCelsius = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 21,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Display Temp Celsius",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });

        /// <summary>
        /// Gets the analog join configuration for LED temperature feedback in Fahrenheit.
        /// Provides the current LED temperature reading converted to Fahrenheit units.
        /// </summary>
        /// <value>Join data for LED temperature in Fahrenheit (Join 22).</value>
        [JoinName("LedTemperatureFahrenheit")]
        public JoinDataComplete LedTemperatureFahrenheit = new JoinDataComplete(
                new JoinData
                {
                    JoinNumber = 22,
                    JoinSpan = 1
                },
            new JoinMetadata
            {
                Description = "Display Temp Fahrenheit",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });


        /// <summary>
        /// Gets the analog join configuration for communication monitor status feedback.
        /// Reports the current status of the communication monitor (online, offline, error states).
        /// </summary>
        /// <value>Join data for communication status (Join 50).</value>
        [JoinName("Status")]
        public JoinDataComplete Status = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 50,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Display communication monitor status",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });

        /// <summary>
        /// Gets the digital join configuration for volume controls visibility feedback.
        /// Indicates whether volume controls should be visible in the user interface.
        /// </summary>
        /// <value>Join data for volume controls visibility (Join 40).</value>
        [JoinName("VolumeControlsVisibleFb")]
        public JoinDataComplete VolumeControlsVisibleFb = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 40,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Enable Visibility of Volume Controls",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });


        /// <summary>
        /// Initializes a new instance of the SamsungDisplayControllerJoinMap class with the specified join start number.
        /// Inherits standard display controller joins and adds Samsung-specific temperature and status joins.
        /// </summary>
        /// <param name="joinStart">The starting join number for this device's join mapping.</param>
        public SamsungDisplayControllerJoinMap(uint joinStart)
            : base(joinStart, typeof(SamsungDisplayControllerJoinMap))
        {
        }
    }
}