﻿using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;                          				// For Basic SIMPL# Classes
// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.Routing;
using PepperDash.Essentials.Bridges;
using Newtonsoft.Json;

namespace PepperDash.Plugin.Display.SamsungMdc
{
	//public class PdtSamsungMdcDisplay : TwoWayDisplayBase, IBasicVolumeWithFeedback, ICommunicationMonitor, IInputDisplayPort1, IInputDisplayPort2,
	//    IInputHdmi1, IInputHdmi2, IInputHdmi3, IInputHdmi4
	public class PdtSamsungMdcDisplay : TwoWayDisplayBase, IBasicVolumeWithFeedback, ICommunicationMonitor, IBridge
	{

		public static void LoadPlugin()
		{
			PepperDash.Essentials.Core.DeviceFactory.AddFactoryForType("samsungmdcplugin", PdtSamsungMdcDisplay.BuildDevice);
		}

		public static string MinimumEssentialsFrameworkVersion = "1.4.31";

		public static PdtSamsungMdcDisplay BuildDevice(DeviceConfig dc)
		{
			var config = JsonConvert.DeserializeObject<DeviceConfig>(dc.Properties.ToString());
			var newMe = new PdtSamsungMdcDisplay(dc);
			return newMe;
		}

		public IBasicCommunication Communication { get; private set; }
		public StatusMonitorBase CommunicationMonitor { get; private set; }

		public byte ID { get; private set; }

		bool LastCommandSentWasVolume;

		bool _PowerIsOn;
		bool _IsWarmingUp;
		bool _IsCoolingDown;
		ushort _VolumeLevelForSig;
		int _LastVolumeSent;
		bool _IsMuted;
		ushort _MaxTemperature;
		ushort _CurrentTemperature;
		RoutingInputPort _CurrentInputPort;
		byte[] IncomingBuffer = new byte[] { };
		ActionIncrementer VolumeIncrementer;
		bool VolumeIsRamping;
		public bool IsInStandby { get; private set; }
		bool IsPoweringOnIgnorePowerFb;

        CTimer PollRing;

        public List<BoolFeedback> InputFeedback;
        public List<bool> _InputFeedback;
		public IntFeedback InputNumberFeedback;
		public static List<string> InputKeys = new List<string>();
		public const int InputPowerOn = 101;
		public const int InputPowerOff = 102;
		private int _InputNumber;
		public int InputNumber
		{
			get
			{
				return this._InputNumber;
			}
			set
			{
				this._InputNumber = value;
				InputNumberFeedback.FireUpdate();
                UpdateBooleanFeedback(value);
			}
		}

        

		public IntFeedback CurrentTemperatureFeedback;

		#region Command Constants
		/// <summary>
		/// Header byte
		/// </summary>
		public const byte Header = 0xAA;
		/// <summary>
		/// Status control (Cmd: 0x00) pdf page 26
		/// Gets the current status, status includes: val1=Power, val2=Volume, val3=Mute, val4=Input, val5=Aspect, val6=N Time NF, val7=F Time NF
		/// </summary>
		public const byte StatusControlCmd = 0x00;
		/// <summary>
		/// Status control data 1 - get
		/// </summary>
		public const byte StatusControlGet = 0x00;
		/// <summary>
		/// Display status control (Cmd: 0x0D) pdf page 34
		/// Gets the display status, status includes: val1=Lamp, val2=Temperature, val3=Bright_Sensor, val4=No_Sync, val5=Current_Temp, val6=Fan
		/// </summary>
		public const byte DisplayStatusControlCmd = 0x0D;
		/// <summary>
		/// Power control (Cmd: 0x11) pdf page 42
		/// Gets/sets the power state
		/// </summary>
		public const byte PowerControlCmd = 0x11;
		/// <summary>
		/// Power control data1 - on 
		/// </summary>
		public const byte PowerControlOn = 0x01;
		/// <summary>
		/// Power control data1 - off
		/// </summary>
		public const byte PowerControlOff = 0x00;
		/// <summary>
		/// Volume level control (Cmd: 0x12) pdf page 44
		/// Gets/sets the volume level
		/// Level range 0d - 100d (0x00 - 0x64)
		/// </summary>
		public const byte VolumeLevelControlCmd = 0x12;
		/// <summary>
		/// Volume mute control (Cmd: 0x13) pdf page 45
		/// Gets/sets the volume mute state
		/// </summary>
		public const byte VolumeMuteControlCmd = 0x13;
		/// <summary>
		/// Volume mute control data1 - on 
		/// </summary>
		public const byte VolumeMuteControlOn = 0x01;
		/// <summary>
		/// Volume mute control data1 - off
		/// </summary>
		public const byte VolumeMuteControlOff = 0x00;
		/// <summary>
		/// Input source control (Cmd: 0x14) pdf page 46
		/// Gets/sets the input state
		/// </summary>
		public const byte InputControlCmd = 0x14;
		/// <summary>
		/// Input source control data1 - S-Video1
		/// </summary>
		public const byte InputControlSvideo1 = 0x04;
		/// <summary>
		/// Input source control data1 - Component1
		/// </summary>
		public const byte InputControlComponent1 = 0x08;
		/// <summary>
		/// Input source control data1 - AV1
		/// </summary>
		public const byte InputControlAv1 = 0x0C;
		/// <summary>
		/// Input source control data1 - AV2
		/// </summary>
		public const byte InputControlAv2 = 0x0D;
		/// <summary>
		/// Input source control data1 - Scart1
		/// </summary>
		public const byte InputControlScart1 = 0x0E;
		/// <summary>
		/// Input source control data1 - DVI1
		/// </summary>
		public const byte InputControlDvi1 = 0x18;
		/// <summary>
		/// Input source control data1 - PC1
		/// </summary>
		public const byte InputControlPc1 = 0x14;
		/// <summary>
		/// Input source control data1 - BNC1
		/// </summary>
		public const byte InputControlBnc1 = 0x1E;
		/// <summary>
		/// Input source control data1 - DVI Video1
		/// </summary>
		public const byte InputControlDviVideo1 = 0x1F;
		/// <summary>
		/// Input source control data1 - HDMI1
		/// </summary>
		public const byte InputControlHdmi1 = 0x21;
		/// <summary>
		/// Input source control data1 - HDMI1 PC
		/// </summary>
		public const byte InputControlHdmi1Pc = 0x22;
		/// <summary>
		/// Input source control data1 - HDMI2
		/// </summary>
		public const byte InputControlHdmi2 = 0x23;
		/// <summary>
		/// Input source control data1 - HDMI2 PC
		/// </summary>
		public const byte InputControlHdmi2Pc = 0x24;
		/// <summary>
		/// Input source control data1 - DisplayPort1
		/// </summary>
		public const byte InputControlDisplayPort1 = 0x25;
		/// <summary>
		/// Input source control data1 - DisplayPort2
		/// </summary>
		public const byte InputControlDisplayPort2 = 0x26;
		/// <summary>
		/// Input source control data1 - DisplayPort3
		/// </summary>
		public const byte InputControlDisplayPort3 = 0x27;
		/// <summary>
		/// Input source control data1 - HDMI3
		/// </summary>
		public const byte InputControlHdmi3 = 0x31;
		/// <summary>
		/// Input source control data1 - HDMI3 PC
		/// </summary>
		public const byte InputControlHdmi3Pc = 0x32;
		/// <summary>
		/// Input source control data1 - HDMI4
		/// </summary>
		public const byte InputControlHdmi4 = 0x33;
		/// <summary>
		/// Input source control data1 - HDMI4 PC
		/// </summary>
		public const byte InputControlHdmi4Pc = 0x34;
		/// <summary>
		/// Input source control data1 - TV1
		/// </summary>
		public const byte InputControlTv1 = 0x40;
		/// <summary>
		/// Input source control data1 - HDBase-T1
		/// </summary>
		public const byte InputControlHdBaseT1 = 0x55;
		/// <summary>
		/// Picture size control (Cmd: 0x15) pdf page 48
		/// Gets/sets the picture size state
		/// </summary>
		public const byte AspectControlCmd = 0x15;
		/// <summary>
		/// Picture Size control data1 - PC 16x9
		/// </summary>
		public const byte AspectControlPc16x9 = 0x10;
		/// <summary>
		/// Picture Size control data1 - PC 4x3
		/// </summary>
		public const byte AspectControlPc4x3 = 0x18;
		/// <summary>
		/// Picture Size control data1 - PC Original
		/// </summary>
		public const byte AspectControlPcOriginal = 0x20;
		/// <summary>
		/// Picture Size control data1 - PC 21x9
		/// </summary>
		public const byte AspectControlPc21x9 = 0x21;
		/// <summary>
		/// Picture Size control data1 - PC Custom
		/// </summary>
		public const byte AspectControlPcCustom = 0x22;
		/// <summary>
		/// Picture Size control data1 - Video Auto Wide
		/// </summary>
		public const byte AspectControlVideoAutoWide = 0x00;
		/// <summary>
		/// Picture Size control data1 - Video 16x9
		/// </summary>
		public const byte AspectControlVideo16x9 = 0x01;
		/// <summary>
		/// Picture Size control data1 - Video Zoom
		/// </summary>
		public const byte AspectControlVideoZoom = 0x04;
		/// <summary>
		/// Picture Size control data1 - Video Zoom1
		/// </summary>
		public const byte AspectControlVideoZoom1 = 0x05;
		/// <summary>
		/// Picture Size control data1 - Video Zoom2
		/// </summary>
		public const byte AspectControlVideoZoom2 = 0x06;
		/// <summary>
		/// Picture Size control data1 - Video Justified
		/// </summary>
		public const byte AspectControlVideoJustified = 0x09;
		/// <summary>
		/// Picture Size control data1 - Video 4x3
		/// </summary>
		public const byte AspectControlVideo4x3 = 0x0B;
		/// <summary>
		/// Picture Size control data1 - Video Wide Fit
		/// </summary>
		public const byte AspectControlVideoWideFit = 0x0C;
		/// <summary>
		/// Picture Size control data1 - Video Custom
		/// </summary>
		public const byte AspectControlVideoCustom = 0x0D;
		/// <summary>
		/// Picture Size control data1 - Video SmartView1
		/// </summary>
		public const byte AspectControlVideoSmartView1 = 0x0E;
		/// <summary>
		/// Picture Size control data1 - Video SmartView2
		/// </summary>
		public const byte AspectControlVideoSmartView2 = 0x0F;
		/// <summary>
		/// Picture Size control data1 - Video Wide Zoom
		/// </summary>
		public const byte AspectControlVideoWideZoom = 0x31;
		/// <summary>
		/// Picture Size control data1 - Video 21x9
		/// </summary>
		public const byte AspectControlVideo21x9 = 0x32;
		/// <summary>
		/// Brightness Control (Cmd: 0x25) pdf page 77
		/// Gets/sets the brightness level
		/// Level range 0d - 100d (0x00 - 0x64)
		/// </summary>
		public const byte BrightnessControlCmd = 0x25;
		/// <summary>
		/// Volume increment/decrement control (Cmd: 0x62) pdf page 122
		/// Set only, increments/decrements the volume level
		/// </summary>
		public const byte VolumeAdjustCmd = 0x62;
		/// <summary>
		/// Volume increment/decrement control data1 - up
		/// </summary>
		public const byte VolumeAdjustUp = 0x00;
		/// <summary>
		/// Volume increment/decrement control data1 - down
		/// </summary>
		public const byte VolumeAdjustDown = 0x01;
		/// <summary>
		/// Temeprature Control (Cmd: 0x85) pdf page 142
		/// Gets/sets the max temp threshold
		/// Temp Range 75C - 124C
		/// </summary>
		public const byte TemerpatureMaxControlCmd = 0x85;
		/// <summary>
		/// Virtual remote control (Cmd: 0xB0) pdf pg. 81
		/// Set only, emulates the IR remote
		/// </summary>
		public const byte VirtualRemoteCmd = 0xB0;
		/// <summary>
		/// Virtual remote control data1 (keyCode) - Menu (0x1A)
		/// </summary>
		public const byte VirtualRemoteMenu = 0x1A;
		/// <summary>
		/// Virtual remote control data1 (keyCode) - Dpad Up (0x60)
		/// </summary>
		public const byte VirtualRemoteUp = 0x60;
		/// <summary>
		/// Virtual remote control data1 (keyCode) - Dpad Down (0x61)
		/// </summary>
		public const byte VirtualRemoteDown = 0x61;
		/// <summary>
		/// Virtual remote control data1 (keyCode) - Dpad Left (0x65)
		/// </summary>
		public const byte VirtualRemoteLeft = 0x65;
		/// <summary>
		/// Virtual remote control data1 (keyCode) - Dpad Right (0x62)
		/// </summary>
		public const byte VirtualRemoteRight = 0x62;
		/// <summary>
		/// Virtual remote control data1 (keyCode) - Dpad Selct (0x68)
		/// </summary>
		public const byte VirtualRemoteSelect = 0x68;
		/// <summary>
		/// Virtual remote control data1 (keyCode) - Exit (0x2D)
		/// </summary>
		public const byte VirtualRemoteExit = 0x2D;
		/// <summary>
		/// Led Product Feature (Cmd: 0xD0) pdg page 221
		/// LED Product Features has a subset of commands available
		/// </summary>
		public const byte LedProductCmd = 0xD0;
		/// <summary>
		/// Monitoring Temperature (Sub Cmd: 0x84) pdf page 228		
		/// Gets LED Product status, status includes: val1=Power&IC, val2=HDBaseT_Status, val3=Temperature, val4=Illuminance, val5=Module1, val6=Module1_LED_Error_Data,.... valN=ModuleX, valN+1=ModuleX_LED_Error_Data\
		/// Temperature range 0C-254C
		/// Illuminance range 0d - 100d (0x00 - 0x64)
		/// </summary>
		public const byte LedProductMonitoringCmd = 0x84;
		#endregion

		protected override Func<bool> PowerIsOnFeedbackFunc { get { return () => _PowerIsOn; } }
		protected override Func<bool> IsCoolingDownFeedbackFunc { get { return () => _IsCoolingDown; } }
		protected override Func<bool> IsWarmingUpFeedbackFunc { get { return () => _IsWarmingUp; } }
		protected override Func<string> CurrentInputFeedbackFunc { get { return () => _CurrentInputPort.Key; } }

		/// <summary>
		/// Constructor for IBaseCommunication
		/// </summary>
		/// <param name="key"></param>
		/// <param name="name"></param>
		/// <param name="config"></param>
		//public PdtSamsungMdcDisplay(string key, string name, DeviceConfig config) : base(key, name)
		public PdtSamsungMdcDisplay(DeviceConfig config)
			: base(config.Key, config.Name)
		{
			Communication = CommFactory.CreateCommForDevice(config);
            Communication.BytesReceived += new EventHandler<GenericCommMethodReceiveBytesArgs>(Communication_BytesReceived);
			var props = config.Properties.ToObject<SamsungMDCDisplayPropertiesConfig>();
			if (props == null || props.Id == null)
			{
				ID = (byte)0x01;
			}
			else
			{
				ID = Convert.ToByte(props.Id, 16);
			}
            _InputFeedback = new List<bool>();
            InputFeedback = new List<BoolFeedback>();
            for (int i = 0; i < 8; i++)
            {
                int j = i;
                _InputFeedback.Add(false);
                InputFeedback.Add(new BoolFeedback(() => _InputFeedback[j]));
            }

			Init();
		}		

		/// <summary>
		/// Add routing input port 
		/// </summary>
		/// <param name="port"></param>
		/// <param name="fbMatch"></param>
		void AddRoutingInputPort(RoutingInputPort port, byte fbMatch)
		{
			port.FeedbackMatchObject = fbMatch;
			InputPorts.Add(port);
		}

		/// <summary>
		/// Initialize 
		/// </summary>
		void Init()
		{
			WarmupTime = 10000;
			CooldownTime = 8000;

			//TODO: determine your poll rate the first value in teh GenericCommunicationMonitor, currently 45s (45,000)
			CommunicationMonitor = new GenericCommunicationMonitor(this, Communication, 10000, 180000, 300000, StatusGet);
			DeviceManager.AddDevice(CommunicationMonitor);

			VolumeIncrementer = new ActionIncrementer(655, 0, 65535, 800, 80,
				v => SetVolume((ushort)v),
				() => _LastVolumeSent);

			AddRoutingInputPort(new RoutingInputPort(RoutingPortNames.HdmiIn1, eRoutingSignalType.Audio | eRoutingSignalType.Video,
				eRoutingPortConnectionType.Hdmi, new Action(InputHdmi1), this), InputControlHdmi1);

			AddRoutingInputPort(new RoutingInputPort(RoutingPortNames.HdmiIn2, eRoutingSignalType.Audio | eRoutingSignalType.Video,
				eRoutingPortConnectionType.Hdmi, new Action(InputHdmi2), this), InputControlHdmi2);

			AddRoutingInputPort(new RoutingInputPort(RoutingPortNames.HdmiIn3, eRoutingSignalType.Audio | eRoutingSignalType.Video,
				eRoutingPortConnectionType.Hdmi, new Action(InputHdmi3), this), InputControlHdmi3);

			AddRoutingInputPort(new RoutingInputPort(RoutingPortNames.HdmiIn4, eRoutingSignalType.Audio | eRoutingSignalType.Video,
				eRoutingPortConnectionType.Hdmi, new Action(InputHdmi4), this), InputControlHdmi4);

			AddRoutingInputPort(new RoutingInputPort(RoutingPortNames.DisplayPortIn1, eRoutingSignalType.Audio | eRoutingSignalType.Video,
				eRoutingPortConnectionType.DisplayPort, new Action(InputDisplayPort1), this), InputControlDisplayPort1);

			AddRoutingInputPort(new RoutingInputPort(RoutingPortNames.DisplayPortIn2, eRoutingSignalType.Audio | eRoutingSignalType.Video,
				eRoutingPortConnectionType.DisplayPort, new Action(InputDisplayPort2), this), InputControlDisplayPort2);

			AddRoutingInputPort(new RoutingInputPort(RoutingPortNames.DviIn, eRoutingSignalType.Audio | eRoutingSignalType.Video,
				eRoutingPortConnectionType.Dvi, new Action(InputDvi1), this), InputControlDvi1);

			StatusGet();

			VolumeLevelFeedback = new IntFeedback(() => { return _VolumeLevelForSig; });
			MuteFeedback = new BoolFeedback(() => _IsMuted);
			InputNumberFeedback = new IntFeedback(() => { Debug.Console(2, this, "Change Input number {0}", _InputNumber); return _InputNumber; });
			CurrentTemperatureFeedback = new IntFeedback(() => { Debug.Console(2, this, "Current Temperature {0}", _CurrentTemperature); return _CurrentTemperature; });
		}

		/// <summary>
		/// Custom activate
		/// </summary>
		/// <returns></returns>
		public override bool CustomActivate()
		{
			Communication.Connect();
			CommunicationMonitor.StatusChange += (o, a) => { Debug.Console(2, this, "Communication monitor state: {0}", CommunicationMonitor.Status); };
			CommunicationMonitor.Start();
			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		public override FeedbackCollection<PepperDash.Essentials.Core.Feedback> Feedbacks
		{
			get
			{
				var list = base.Feedbacks;
				list.AddRange(new List<PepperDash.Essentials.Core.Feedback>
				{
                    VolumeLevelFeedback,
                    MuteFeedback,
                    CurrentInputFeedback,
					CurrentTemperatureFeedback
				});
				return list;
			}
		}

		/// <summary>
		/// Communication bytes recieved
		/// </summary>
		/// <param name="sender"></param>
		void Communication_BytesReceived(object sender, GenericCommMethodReceiveBytesArgs e)       
        {
			// This is probably not thread-safe buffering
			// Append the incoming bytes with whatever is in the buffer
			var newBytes = new byte[IncomingBuffer.Length + e.Bytes.Length];
			IncomingBuffer.CopyTo(newBytes, 0);
			e.Bytes.CopyTo(newBytes, IncomingBuffer.Length);

			if (Debug.Level == 2) // This check is here to prevent following string format from building unnecessarily on level 0 or 1
				Debug.Console(2, this, "Received:{0}", ComTextHelper.GetEscapedText(newBytes));

			// Need to find AA FF and have 
			for (int i = 0; i < newBytes.Length; i++)
			{
				if (newBytes[i] == 0xAA && newBytes[i + 1] == 0xFF)
				{
					newBytes = newBytes.Skip(i).ToArray(); // Trim off junk if there's "dirt" in the buffer

					// parse it
					// If it's at least got the header, then process it, 
					while (newBytes.Length > 4 && newBytes[0] == Header && newBytes[1] == 0xFF)
					{
						var msgLen = newBytes[3];
						// if the buffer is shorter than the header (3) + message (msgLen) + checksum (1),
						// give and save it for next time 
						if (newBytes.Length < msgLen + 4)
							break;

						// Good length, grab the message
						var message = newBytes.Skip(4).Take(msgLen).ToArray();

						// At this point, the ack/nak is the first byte
						if (message[0] == 0x41)
						{
							// type byte
							switch (message[1])
							{
								// General status
								case StatusControlCmd:
									{
										//UpdatePowerFB(message[2], message[5]); // "power" can be misrepresented when the display sleeps
										// Handle the first power on fb when waiting for it.
										if (IsPoweringOnIgnorePowerFb && message[2] == PowerControlOn)
											IsPoweringOnIgnorePowerFb = false;
										// Ignore general-status power off messages when powering up
										if (!(IsPoweringOnIgnorePowerFb && message[2] == PowerControlOff))
											UpdatePowerFB(message[2]);
										UpdateVolumeFB(message[3]);
										UpdateMuteFb(message[4]);
										UpdateInputFb(message[5]);
										break;
									}
								// Power status
								case PowerControlCmd:
									{
										UpdatePowerFB(message[2]);
										break;
									}
								// Volume level
								case VolumeLevelControlCmd:
									{
										UpdateVolumeFB(message[2]);
										break;
									}
								// Volume mute status
								case VolumeMuteControlCmd:
									{
										UpdateMuteFb(message[2]);
										break;
									}
								// Input status
								case InputControlCmd:
									{
										UpdateInputFb(message[2]);
										break;
									}
								default:
									{
										break;
									}
							}
						}
						// Skip over what we've used and save the rest for next time
						newBytes = newBytes.Skip(5 + msgLen).ToArray();
					}
					break; // parsing will mean we can stop looking for header in loop
				}
			}

			// Save whatever partial message is here
			IncomingBuffer = newBytes;
		}

		/// <summary>
		/// Power feedback
		/// </summary>
		void UpdatePowerFB(byte powerByte)
		{
			var newVal = powerByte == 1;
			if (newVal != _PowerIsOn)
			{
				_PowerIsOn = newVal;
				PowerIsOnFeedback.FireUpdate();
			}
		}

		/// <summary>
		/// Updates power status from general updates where source is included.
		/// Compensates for errant standby / power off hiccups by ignoring 
		/// power off states with input < 0x10 
		/// </summary>
		void UpdatePowerFB(byte powerByte, byte inputByte)
		{
			// This should reject errant power feedbacks when switching away from input on standby.
			if (powerByte == 0x01 && inputByte < 0x10)
				IsInStandby = true;
			if (powerByte == 0x00 && IsInStandby) // Ignore power off if coming from standby - glitch
			{
				IsInStandby = false;
				return;
			}

			UpdatePowerFB(powerByte);
		}

		/// <summary>
		/// Volume feedback
		/// </summary>
		void UpdateVolumeFB(byte b)
		{
			var newVol = (ushort)NumericalHelpers.Scale((double)b, 0, 100, 0, 65535);
			if (!VolumeIsRamping)
				_LastVolumeSent = newVol;
			if (newVol != _VolumeLevelForSig)
			{
				_VolumeLevelForSig = newVol;
				VolumeLevelFeedback.FireUpdate();
			}
		}

		/// <summary>
		/// Mute feedback
		/// </summary>
		void UpdateMuteFb(byte b)
		{
			var newMute = b == 1;
			if (newMute != _IsMuted)
			{
				_IsMuted = newMute;
				MuteFeedback.FireUpdate();
			}
		}

		/// <summary>
		/// Input feedback
		/// </summary>
		void UpdateInputFb(byte b)
		{
			var newInput = InputPorts.FirstOrDefault(i => i.FeedbackMatchObject.Equals(b));
			if (newInput != null && newInput != _CurrentInputPort)
			{
				_CurrentInputPort = newInput;
				CurrentInputFeedback.FireUpdate();
                var key = newInput.Key;
                switch (key)
                {
                    case "hdmiIn1":
                        InputNumber = 1;
                        break;
                    case "hdmiIn2" :
                        InputNumber = 2;
                        break;
                    case "hdmiIn3" :
                        InputNumber = 3;
                        break;
                    case "hdmiIn4" :
                        InputNumber = 4;
                        break;
                    case "displayPortIn1" :
                        InputNumber = 5;
                        break;
                    case "displayPortIn2" :
                        InputNumber = 6;
                        break;
                    case "dviIn" :
                        InputNumber = 7;
                        break;
                    default:
                        break;
                }
			}
		}

		/// <summary>
		/// Formats an outgoing message. 
		/// Third byte will be replaced with ID and last byte will be replaced with calculated checksum.
		/// All bytes to make a valid message must be included and can be represented with 0x00. 
		/// Get ex. [HEADER][CMD][ID][DATA_LEN][CS]
		/// Set ex. [HEADER][CMD][ID][DATA_LEN][DATA-1...DATA-N][CS]
		/// </summary>
		/// <param name="b">byte array</param>
		void SendBytes(byte[] b)
		{
			// Command structure 
			// [HEADER][CMD][ID][DATA_LEN][DATA-1]....[DATA-N][CHK_SUM]
			// PowerOn ex: 0xAA,0x11,0x01,0x01,0x01,0x01
			if (LastCommandSentWasVolume)   // If the last command sent was volume
				if (b[1] != 0x12)           // Check if this command is volume, and if not, delay this command 
					CrestronEnvironment.Sleep(100);

			b[2] = ID;
			// append checksum by adding all bytes, except last which should be 00
			int checksum = 0;
			for (var i = 1; i < b.Length - 1; i++) // add 2nd through 2nd-to-last bytes
			{
				checksum += b[i];
			}
			checksum = checksum & 0x000000FF; // mask off MSBs
			b[b.Length - 1] = (byte)checksum;
			if (Debug.Level == 2) // This check is here to prevent following string format from building unnecessarily on level 0 or 1
				Debug.Console(2, this, "Sending:{0}", ComTextHelper.GetEscapedText(b));

			if (b[1] == 0x12)
				LastCommandSentWasVolume = true;
			else
				LastCommandSentWasVolume = false;

			Communication.SendBytes(b);
		}

		/// <summary>
		/// Status control (Cmd: 0x00) pdf page 26
		/// Get: [HEADER=0xAA][Cmd=0x00][ID][DATA_LEN=0x00][CS=0x00]
		/// </summary>
		public void StatusGet()
		{
			//SendBytes(new byte[] { Header, StatusControlCmd, 0x00, 0x00, StatusControlGet, 0x00 });
            
            PowerGet();
            if (PollRing != null) PollRing = null;
            PollRing = new CTimer(o => InputGet(), null, 1000);
            
		}

		/// <summary>
		/// Power on (Cmd: 0x11) pdf page 42 
		/// Set: [HEADER=0xAA][Cmd=0x11][ID][DATA_LEN=0x01][DATA-1=0x01][CS=0x00]
		/// </summary>
		public override void PowerOn()
		{
			IsPoweringOnIgnorePowerFb = true;
			SendBytes(new byte[] { Header, PowerControlCmd, 0x00, 0x01, PowerControlOn, 0x00 });
			if (!PowerIsOnFeedback.BoolValue && !_IsWarmingUp && !_IsCoolingDown)
			{
				_IsWarmingUp = true;
				IsWarmingUpFeedback.FireUpdate();
				// Fake power-up cycle
				WarmupTimer = new CTimer(o =>
					{
						_IsWarmingUp = false;
						_PowerIsOn = true;
						IsWarmingUpFeedback.FireUpdate();
						PowerIsOnFeedback.FireUpdate();
					}, WarmupTime);
			}
		}

		/// <summary>
		/// Power off (Cmd: 0x11) pdf page 42 
		/// Set: [HEADER=0xAA][Cmd=0x11][ID][DATA_LEN=0x01][DATA-1=0x00][CS=0x00]
		/// </summary>
		public override void PowerOff()
		{
			IsPoweringOnIgnorePowerFb = false;
			// If a display has unreliable-power off feedback, just override this and
			// remove this check.
			if (!_IsWarmingUp && !_IsCoolingDown) // PowerIsOnFeedback.BoolValue &&
			{
				SendBytes(new byte[] { Header, PowerControlCmd, 0x00, 0x01, PowerControlOff, 0x00 });
				_IsCoolingDown = true;
				_PowerIsOn = false;
				PowerIsOnFeedback.FireUpdate();
				IsCoolingDownFeedback.FireUpdate();
				// Fake cool-down cycle
				CooldownTimer = new CTimer(o =>
					{
						_IsCoolingDown = false;
						IsCoolingDownFeedback.FireUpdate();
					}, CooldownTime);
			}
		}

        private void UpdateBooleanFeedback(int data)
        {
            if (_InputFeedback[data] == true)
                return;
            else
            {
                for (int i = 1; i < 8; i++)
                {
                    _InputFeedback[i] = false;
                }
                _InputFeedback[data] = true;
                foreach (var item in InputFeedback)
                {
                    item.FireUpdate();
                }
            }
        }

		/// <summary>		
		/// Power toggle (Cmd: 0x11) pdf page 42 
		/// Set: [HEADER=0xAA][Cmd=0x11][ID][DATA_LEN=0x01][DATA-1=0x01||0x00][CS=0x00]
		/// </summary>
		public override void PowerToggle()
		{
			if (PowerIsOnFeedback.BoolValue && !IsWarmingUpFeedback.BoolValue)
				PowerOff();
			else if (!PowerIsOnFeedback.BoolValue && !IsCoolingDownFeedback.BoolValue)
				PowerOn();
		}

		/// <summary>
		/// Power on (Cmd: 0x11) pdf page 42 
		/// Get: [HEADER=0xAA][Cmd=0x11][ID][DATA_LEN=0x00][CS=0x00]		
		/// </summary>
		public void PowerGet()
		{
			//SendBytes(PowerGetCmd);
			SendBytes(new byte[] { Header, PowerControlCmd, 0x00, 0x00, 0x00 });
		}

		/// <summary>
		/// Input HDMI 1 (Cmd: 0x14) pdf page 426
		/// Set: [HEADER=0xAA][Cmd=0x14][ID][DATA_LEN=0x01][DATA-1=0x21][CS=0x00]
		/// </summary>
		public void InputHdmi1()
		{
			SendBytes(new byte[] { Header, InputControlCmd, 0x00, 0x01, InputControlHdmi1, 0x00 });
		}

		/// <summary>
		/// Input HDMI 2 (Cmd: 0x14) pdf page 426
		/// Set: [HEADER=0xAA][Cmd=0x14][ID][DATA_LEN=0x01][DATA-1=0x23][CS=0x00]
		/// </summary>
		public void InputHdmi2()
		{
			SendBytes(new byte[] { Header, InputControlCmd, 0x00, 0x01, InputControlHdmi2, 0x00 });
		}

		/// <summary>
		/// Input HDMI 3 (Cmd: 0x14) pdf page 426
		/// Set: [HEADER=0xAA][Cmd=0x14][ID][DATA_LEN=0x01][DATA-1=0x31][CS=0x00]
		/// </summary>
		public void InputHdmi3()
		{
			SendBytes(new byte[] { Header, InputControlCmd, 0x00, 0x01, InputControlHdmi3, 0x00 });
		}

		/// <summary>
		/// Input HDMI 4 (Cmd: 0x14) pdf page 426
		/// Set: [HEADER=0xAA][Cmd=0x14][ID][DATA_LEN=0x01][DATA-1=0x33][CS=0x00]
		/// </summary>
		public void InputHdmi4()
		{
			SendBytes(new byte[] { Header, InputControlCmd, 0x00, 0x01, InputControlHdmi4, 0x00 });
		}

		/// <summary>
		/// Input DisplayPort 1 (Cmd: 0x14) pdf page 426
		/// Set: [HEADER=0xAA][Cmd=0x14][ID][DATA_LEN=0x01][DATA-1=0x25][CS=0x00]
		/// </summary>
		public void InputDisplayPort1()
		{
			SendBytes(new byte[] { Header, InputControlCmd, 0x00, 0x01, InputControlDisplayPort1, 0x00 });
		}

		/// <summary>
		/// Input DisplayPort 2 (Cmd: 0x14) pdf page 426
		/// Set: [HEADER=0xAA][Cmd=0x14][ID][DATA_LEN=0x01][DATA-1=0x26][CS=0x00]
		/// </summary>
		public void InputDisplayPort2()
		{
			SendBytes(new byte[] { Header, InputControlCmd, 0x00, 0x01, InputControlDisplayPort2, 0x00 });
		}

		/// <summary>
		/// Input DVI 1 (Cmd: 0x14) pdf page 426
		/// Set: [HEADER=0xAA][Cmd=0x14][ID][DATA_LEN=0x01][DATA-1=0x18][CS=0x00]
		/// </summary>
		public void InputDvi1()
		{
			SendBytes(new byte[] { Header, InputControlCmd, 0x00, 0x01, InputControlDvi1, 0x00 });
		}

		/// <summary>
		/// Input HDMI 1 (Cmd: 0x14) pdf page 426
		/// Get: [HEADER=0xAA][Cmd=0x14][ID][DATA_LEN=0x00][CS=0x00]
		/// </summary>
		public void InputGet()
		{
			SendBytes(new byte[] { Header, InputControlCmd, 0x00, 0x00, 0x00 });
            
            if (PollRing != null) PollRing = null;
            PollRing = new CTimer(o => VolumeGet(), null, 1000);
            
		}

		/// <summary>
		/// Temeprature Control (Cmd: 0x85) pdf page 142
		/// Get: [HEADER=0xAA][Cmd=0x85][ID][DATA_LEN=0x00][CS=0x00]		
		/// </summary>
		public void TemperatureMaxGet()
		{
			SendBytes(new byte[] { Header, TemerpatureMaxControlCmd, 0x00, 0x00, 0x00 });
		}


		/// <summary>
		/// Executes a switch, turning on display if necessary.
		/// </summary>
		/// <param name="selector"></param>
		public override void ExecuteSwitch(object selector)
		{
			//if (!(selector is Action))
			//    Debug.Console(1, this, "WARNING: ExecuteSwitch cannot handle type {0}", selector.GetType());

			if (_PowerIsOn)
				(selector as Action)();
			else // if power is off, wait until we get on FB to send it. 
			{
				// One-time event handler to wait for power on before executing switch
				EventHandler<FeedbackEventArgs> handler = null; // necessary to allow reference inside lambda to handler
				handler = (o, a) =>
				{
					if (!_IsWarmingUp) // Done warming
					{
						IsWarmingUpFeedback.OutputChange -= handler;
						(selector as Action)();
					}
				};
				IsWarmingUpFeedback.OutputChange += handler; // attach and wait for on FB
				PowerOn();
			}
		}

		/// <summary>
		/// Scales the level to the range of the display and sends the command
		/// Volume level control (Cmd: 0x12) pdf page 44
		/// Level range 0d - 100d (0x00 - 0x64)		
		/// Set: [HEADER=0xAA][Cmd=0x12][ID][DATA_LEN=0x01][DATA-1=(Scaled)][CS=0x00]
		/// </summary>
		/// <param name="level"></param>
		public void SetVolume(ushort level)
		{
			_LastVolumeSent = level;
			var scaled = (int)NumericalHelpers.Scale(level, 0, 65535, 0, 100);
			// The inputs to Scale ensure that byte won't overflow
            SendBytes(new byte[] { Header, VolumeLevelControlCmd, 0x00, 0x01, Convert.ToByte(scaled), 0x00 });
		}

		#region IBasicVolumeWithFeedback Members

		/// <summary>
		/// Volume level feedback property
		/// </summary>
		public IntFeedback VolumeLevelFeedback { get; private set; }
		/// <summary>
		/// volume mte feedback property
		/// </summary>
		public BoolFeedback MuteFeedback { get; private set; }

		/// <summary>
		/// Mute off (Cmd: 0x13) pdf page 45
		/// Set: [Header=0xAA][Cmd=0x13][ID][DATA_LEN=0x01][DATA-1=0x00][CS=0x00]
		/// </summary>
		public void MuteOff()
		{
			SendBytes(new byte[] { Header, VolumeMuteControlCmd, 0x00, 0x01, VolumeMuteControlOff, 0x00 });
		}

		/// <summary>
		/// Mute on (Cmd: 0x13) pdf page 45
		/// Set: [Header=0xAA][Cmd=0x13][ID][DATA_LEN=0x01][DATA-1=0x01][CS=0x00]
		/// </summary>
		public void MuteOn()
		{
			SendBytes(new byte[] { Header, VolumeMuteControlCmd, 0x00, 0x01, VolumeMuteControlOn, 0x00 });
		}

		/// <summary>
		/// Mute get (Cmd: 0x13) pdf page 45
		/// Get: [Header=0xAA][Cmd=0x13][ID][DATA_LEN=0x00][CS=0x00]
		/// </summary>
		public void MuteGet()
		{
			SendBytes(new byte[] { Header, VolumeMuteControlCmd, 0x00, 0x00, 0x00 });
		}

		#endregion

		#region IBasicVolumeControls Members

		/// <summary>
		/// Mute toggle
		/// </summary>
		public void MuteToggle()
		{
			if (_IsMuted)
				MuteOff();
			else
				MuteOn();
		}

		/// <summary>
		/// Volume down (decrement)
		/// </summary>
		/// <param name="pressRelease"></param>
		public void VolumeDown(bool pressRelease)
		{
			if (pressRelease)
			{
				VolumeIncrementer.StartDown();
				VolumeIsRamping = true;
			}
			else
			{
				VolumeIsRamping = false;
				VolumeIncrementer.Stop();
			}
		}

		/// <summary>
		/// Volume up (increment)
		/// </summary>
		/// <param name="pressRelease"></param>
		public void VolumeUp(bool pressRelease)
		{
			if (pressRelease)
			{
				VolumeIncrementer.StartUp();
				VolumeIsRamping = true;
			}
			else
			{
				VolumeIsRamping = false;
				VolumeIncrementer.Stop();
			}
		}

		/// <summary>
		/// Volume level control (Cmd: 0x12) pdf page 44
		/// Level range 0d - 100d (0x00 - 0x64)		
		/// Get: [HEADER=0xAA][Cmd=0x12][ID][DATA_LEN=0x00][CS=0x00]
		/// </summary>
		public void VolumeGet()
		{
            SendBytes(new byte[] { Header, VolumeLevelControlCmd, 0x00, 0x00, 0x00 });
            if (PollRing != null) PollRing = null;
            PollRing = new CTimer(o => MuteGet(), null, 1000);
		}

		#endregion

		#region IBridge Members

		/// <summary>
		/// LinkToApi (bridge method)
		/// </summary>
		/// <param name="trilist"></param>
		/// <param name="joinStart"></param>
		/// <param name="joinMapKey"></param>
		public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey)
		{
			this.LinkToApiExt(trilist, joinStart, joinMapKey);
		}

		#endregion
	}
}