namespace PepperDashPluginSamsungMdcDisplay
{
    /// <summary>
    /// Contains constant values for Samsung MDC (Multiple Display Control) protocol commands and parameters.
    /// These constants represent the command bytes, data values, and control codes used in MDC communication.
    /// All values are documented according to Samsung's MDC protocol specification.
    /// </summary>
    public class SamsungMdcCommands
    {
        /// <summary>
        /// Header byte that starts all MDC protocol messages. Always 0xAA for Samsung MDC.
        /// </summary>
        public const byte Header = 0xAA;

        /// <summary>
        /// Command byte for status control operations (0x00). 
        /// Gets the current status including power, volume, mute, input, aspect ratio, and timing information.
        /// </summary>
        public const byte StatusControl = 0x00;

        /// <summary>
        /// Response command byte for status control operations (0x00).
        /// </summary>
        public const byte StatusControlRcmd = 0x00;

        /// <summary>
        /// Command byte for display status control operations (0x0D).
        /// Gets display-specific status including lamp errors, temperature errors, brightness sensor status, 
        /// sync errors, current temperature, and fan errors.
        /// </summary>
        public const byte DisplayStatusControl = 0x0D;

        /// <summary>
        /// Command byte for power control operations (0x11).
        /// Used to get or set the power state of the display device.
        /// </summary>
        public const byte PowerControl = 0x11;

        /// <summary>
        /// Data value for power on command (0x01). Used with PowerControl command.
        /// </summary>
        public const byte PowerOn = 0x01;

        /// <summary>
        /// Data value for power off command (0x00). Used with PowerControl command.
        /// </summary>
        public const byte PowerOff = 0x00;

        /// <summary>
        /// Command byte for volume control operations (0x12).
        /// Used to get or set the volume level. Volume range is 0-100 (0x00-0x64).
        /// </summary>
        public const byte VolumeControl = 0x12;

        /// <summary>
        /// Command byte for mute control operations (0x13).
        /// Used to get or set the audio mute state of the display.
        /// </summary>
        public const byte MuteControl = 0x13;

        /// <summary>
        /// Data value for mute on command (0x01). Used with MuteControl command.
        /// </summary>
        public const byte MuteOn = 0x01;

        /// <summary>
        /// Data value for mute off command (0x00). Used with MuteControl command.
        /// </summary>
        public const byte MuteOff = 0x00;

        /// <summary>
        /// Command byte for input source control operations (0x14).
        /// Used to get or set the active input source on the display.
        /// </summary>
        public const byte InputSourceControl = 0x14;

        /// <summary>
        /// 2.1.14 Input source control - S-Video1
        /// </summary>
        public const byte InputSvideo1 = 0x04;

        /// <summary>
        /// 2.1.14 Input source control - AV1
        /// </summary>
        public const byte InputAv1 = 0x0C;

        /// <summary>
        /// 2.1.14 Input source control - Component1
        /// </summary>
        public const byte InputComponent1 = 0x08;
        
        /// <summary>
        /// 2.1.14 Input source control - AV2
        /// </summary>
        public const byte InputAv2 = 0x0D;

        /// <summary>
        /// 2.1.14 Input source control - Scart1
        /// </summary>
        public const byte InputScart1 = 0x0E;

        /// <summary>
        /// Input source value for DVI port 1 (0x18). Used with InputSourceControl command.
        /// </summary>
        public const byte InputDvi1 = 0x18;

        /// <summary>
        /// Input source value for PC port 1 (0x14). Used with InputSourceControl command.
        /// </summary>
        public const byte InputPc1 = 0x14;

        /// <summary>
        /// Input source value for BNC port 1 (0x1E). Used with InputSourceControl command.
        /// </summary>
        public const byte InputBnc1 = 0x1E;

        /// <summary>
        /// Input source value for DVI Video port 1 (0x1F). Used with InputSourceControl command.
        /// </summary>
        public const byte InputDviVideo1 = 0x1F;

        /// <summary>
        /// Input source value for HDMI port 1 (0x21). Used with InputSourceControl command.
        /// </summary>
        public const byte InputHdmi1 = 0x21;

        /// <summary>
        /// Input source value for HDMI port 1 in PC mode (0x22). Used with InputSourceControl command.
        /// </summary>
        public const byte InputHdmi1Pc = 0x22;

        /// <summary>
        /// Input source value for HDMI port 2 (0x23). Used with InputSourceControl command.
        /// </summary>
        public const byte InputHdmi2 = 0x23;

        /// <summary>
        /// Input source value for HDMI port 2 in PC mode (0x24). Used with InputSourceControl command.
        /// </summary>
        public const byte InputHdmi2Pc = 0x24;

        /// <summary>
        /// Input source value for DisplayPort 1 (0x25). Used with InputSourceControl command.
        /// </summary>
        public const byte InputDisplayPort1 = 0x25;

        /// <summary>
        /// Input source value for DisplayPort 2 (0x26). Used with InputSourceControl command.
        /// </summary>
        public const byte InputDisplayPort2 = 0x26;

        /// <summary>
        /// Input source value for DisplayPort 3 (0x27). Used with InputSourceControl command.
        /// </summary>
        public const byte InputDisplayPort3 = 0x27;

        /// <summary>
        /// Input source value for HDMI port 3 (0x31). Used with InputSourceControl command.
        /// </summary>
        public const byte InputHdmi3 = 0x31;

        /// <summary>
        /// Input source value for HDMI port 3 in PC mode (0x32). Used with InputSourceControl command.
        /// </summary>
        public const byte InputHdmi3Pc = 0x32;

        /// <summary>
        /// Input source value for HDMI port 4 (0x33). Used with InputSourceControl command.
        /// </summary>
        public const byte InputHdmi4 = 0x33;

        /// <summary>
        /// Input source value for HDMI port 4 in PC mode (0x34). Used with InputSourceControl command.
        /// </summary>
        public const byte InputHdmi4Pc = 0x34;

        /// <summary>
        /// Input source value for MagicInfo player (0x20). Used with InputSourceControl command.
        /// </summary>
        public const byte InputMagicInfo = 0x20;

        /// <summary>
        /// 2.1.14 Input source control - TV1
        /// </summary>
        public const byte InputTv1 = 0x40;

        /// <summary>
        /// 2.1.14 Input source control - HDBase-T1
        /// </summary>
        public const byte InputHdBaseT1 = 0x55;

        /// <summary>
        /// 2.1.15 Picture size control (Cmd: 0x15)
        /// Gets/sets the picture size state
        /// </summary>
        public const byte PictureSizeControl = 0x15;

        /// <summary>
        /// 2.1.15 Picture Size control - PC 16x9
        /// </summary>
        public const byte AspectPc16X9 = 0x10;

        /// <summary>
        /// 2.1.15 Picture Size control - PC 4x3
        /// </summary>
        public const byte AspectPc4X3 = 0x18;

        /// <summary>
        /// 2.1.15 Picture Size control - PC Original
        /// </summary>
        public const byte AspectPcOriginal = 0x20;

        /// <summary>
        /// 2.1.15 Picture Size control - PC 21x9
        /// </summary>
        public const byte AspectPc21X9 = 0x21;

        /// <summary>
        /// 2.1.15 Picture Size control - PC Custom
        /// </summary>
        public const byte AspectPcCustom = 0x22;

        /// <summary>
        /// 2.1.15 Picture Size control - Video Auto Wide
        /// </summary>
        public const byte AspectVideoAutoWide = 0x00;

        /// <summary>
        /// 2.1.15 Picture Size control - Video 16x9
        /// </summary>
        public const byte AspectVideo16X9 = 0x01;

        /// <summary>
        /// 2.1.15 Picture Size control - Video Zoom
        /// </summary>
        public const byte AspectVideoZoom = 0x04;

        /// <summary>
        /// 2.1.15 Picture Size control - Video Zoom1
        /// </summary>
        public const byte AspectVideoZoom1 = 0x05;

        /// <summary>
        /// 2.1.15 Picture Size control - Video Zoom2
        /// </summary>
        public const byte AspectVideoZoom2 = 0x06;

        /// <summary>
        /// 2.1.15 Picture Size control - Video Justified
        /// </summary>
        public const byte AspectVideoJustified = 0x09;

        /// <summary>
        /// 2.1.15 Picture Size control - Video 4x3
        /// </summary>
        public const byte AspectVideo4X3 = 0x0B;

        /// <summary>
        /// 2.1.15 Picture Size control - Video Wide Fit
        /// </summary>
        public const byte AspectVideoWideFit = 0x0C;

        /// <summary>
        /// 2.1.15 Picture Size control - Video Custom
        /// </summary>
        public const byte AspectVideoCustom = 0x0D;

        /// <summary>
        /// 2.1.15 Picture Size control - Video SmartView1
        /// </summary>
        public const byte AspectVideoSmartView1 = 0x0E;

        /// <summary>
        /// 2.1.15 Picture Size control - Video SmartView2
        /// </summary>
        public const byte AspectVideoSmartView2 = 0x0F;

        /// <summary>
        /// 2.1.15 Picture Size control - Video Wide Zoom
        /// </summary>
        public const byte AspectVideoWideZoom = 0x31;

        /// <summary>
        /// 2.1.15 Picture Size control - Video 21x9
        /// </summary>
        public const byte AspectVideo21X9 = 0x32;

        /// <summary>
        /// 2.1.25 Brightness Control (Cmd: 0x25)
        /// Gets/sets the brightness level
        /// Level range 0d - 100d (0x00 - 0x64)
        /// </summary>
        public const byte BrightnessControl = 0x25;

        /// <summary>
        /// 2.1.62 Volume Up/Down (Cmd: 0x62)
        /// Set only, increments/decrements the volume level
        /// </summary>
        public const byte VolumeUpDown = 0x62;

        /// <summary>
        /// 2.1.62 Volume Up/Down - up
        /// </summary>
        public const byte VolumeAdjustUp = 0x00;

        /// <summary>
        /// 2.1.62 Volume Up/Down - down
        /// </summary>
        public const byte VolumeAdjustDown = 0x01;

        /// <summary>
        /// 2.1.85 Temeprature Control (Cmd: 0x85)
        /// Gets/sets the max temp threshold
        /// Temp Range 75C - 124C
        /// </summary>
        public const byte TemperatureControl = 0x85;

        /// <summary>
        /// 2.1.B0 Virtual remote control (Cmd: 0xB0)
        /// Set only, emulates the IR remote
        /// </summary>
        public const byte VirtualRemoteControl = 0xB0;

        /// <summary>
        /// 2.1.B0 Virtual remote control - (keyCode) Menu (0x1A)
        /// </summary>
        public const byte VirtualRemoteMenuKey = 0x1A;

        /// <summary>
        /// 2.1.B0 Virtual remote control - (keyCode) Dpad Up (0x60)
        /// </summary>
        public const byte VirtualRemoteUpKey = 0x60;

        /// <summary>
        /// 2.1.B0 Virtual remote control - (keyCode) Dpad Down (0x61)
        /// </summary>
        public const byte VirtualRemoteDownKey = 0x61;

        /// <summary>
        /// 2.1.B0 Virtual remote control - (keyCode) Dpad Left (0x65)
        /// </summary>
        public const byte VirtualRemoteLeftKey = 0x65;

        /// <summary>
        /// 2.1.B0 Virtual remote control - (keyCode) Dpad Right (0x62)
        /// </summary>
        public const byte VirtualRemoteRightKey = 0x62;

        /// <summary>
        /// 2.1.B0 Virtual remote control - (keyCode) Dpad Selct (0x68)
        /// </summary>
        public const byte VirtualRemoteSelectKey = 0x68;

        /// <summary>
        /// 2.1.B0 Virtual remote control - (keyCode) Exit (0x2D)
        /// </summary>
        public const byte VirtualRemoteExitKey = 0x2D;

        /// <summary>
        /// 2.1.D0 Led Product Feature (Cmd: 0xD0)
        /// LED Product Features has a subset of commands available
        /// </summary>
        public const byte LedProductFeature = 0xD0;

        /// <summary>
        /// 2.1.D0.78 Led Information (LED product feature sub cmd: 0x78)
        /// LED Product Features has a subset of commands available
        /// </summary>
        public const byte LedSubcmdInformation = 0x78;

        /// <summary>
        /// 2.1.D0.84 Led Monitoring (LED product feature sub cmd: 0x84)
        /// Gets LED Product status, status includes: val1=Power_IC, val2=HDBaseT_Status, val3=Temperature, val4=Illuminance, val5=Module1, val6=Module1_LED_Error_Data,.... valN=ModuleX, valN+1=ModuleX_LED_Error_Data\
        /// Temperature range 0C-254C
        /// Illuminance range 0d - 100d (0x00 - 0x64)
        /// </summary>
        public const byte LedSubcmdMonitoring = 0x84;

        public const byte LedSubcmdMonitoringModule1 = 0x1E;
        public const byte LedSubcmdMonitoringModule2 = 0x2E;
        public const byte LedSubcmdMonitoringModule3 = 0x3E;
        public const byte LedSubcmdMonitoringModule4 = 0x4E;
        public const byte LedSubcmdMonitoringModule5 = 0x5E;
        public const byte LedSubcmdMonitoringModule6 = 0x6E;
        public const byte LedSubcmdMonitoringModule7 = 0x7E;
        public const byte LedSubcmdMonitoringModule8 = 0x8E;
        public const byte LedSubcmdMonitoringModule9 = 0x9E;
        public const byte LedSubcmdMonitoringModule10 = 0xAE;
        public const byte LedSubcmdMonitoringModule11 = 0xBE;
        public const byte LedSubcmdMonitoringModule12 = 0xCE;

        /// <summary>
        /// 2.1.0B Serial number control
        /// </summary>
        public const byte SerialNumberControl = 0x0B;

        /// <summary>
        /// 2.1.0E SW version control
        /// </summary>
        public const byte SwVersionControl = 0x0E;

        /// <summary>
        /// 2.1.1B System configuration (cmd: 0x1B)
        /// </summary>
        public const byte SystemConfiguration = 0x1B;

        /// <summary>
        /// 2.1.1B.81 Mac address (system configuration sub-cmd: 0x81)
        /// </summary>
        public const byte SystemConfigurationMacAddress = 0x81;

        /// <summary>
        /// 2.1.1B.82 Network configuration (system configuration sub-cmd: 0x82)
        /// </summary>
        public const byte NetworkConfiguration = 0x82;
    }
}