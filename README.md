![PepperDash Logo](/images/logo_pdt_no_tagline_600.png)
# Samsung MDC Display Plugin

This is a plugin repo for Samsung MDC displays and adds features needed that are not currently part of the Essentials Samsung MDC implementation. To use the plugin follow the steps below.

1. Clone the repo
   - References are managed by nuget. To install the correct references, enter the command below:
     `nuget install .\packages.config -ExcludeVersion -OutputDirectory .\packages`
   - Check the slash direction: if installing from Git Bash, the slashes should be `/` instead of `\`
2. Copy the \*.cplz to th plugin folder created by Essentials
   - ex. \USER\program[X]\plugins\*.cplz
3. Update the Essentials configuration file to include the display objects and display bridge (see examples below)
   - The plugin "type" is set C# to "samsungmdcplugin"
4. Load the Essentials configuration file to the program folder created by Essentials
   - ex. \USER\program[x]\*configurationFile\*.json)
5. Restart Essentials to load the plugin

## Device Specifc Information

This plugin was built using the Samsung SEC-VD-DSW Multiple Display Control document, Ver. 14.4, 2018-4-4.

### RS232 Specification

| Property     | Value |
| ------------ | ----- |
| Baudrate     | 9600  |
| Data Bits    | 8     |
| Parity       | None  |
| Stop Bits    | 1     |
| Flow Control | None  |

### Network Specification

| Property   | Value        |
| ---------- | ------------ |
| Default IP | 192.168.0.10 |
| Port       | 1515         |

## Example Configuration Object

### Display Object using RS-232

```JSON
{
	"key": "Display01",
	"uid": 1,
	"name": "Front Display",
	"type": "samsungmdcplugin",
	"group": "display",
	"properties": {
		"id": "01",
		"control": {
			"method": "com",
			"controlPortDevKey": "processor",
			"controlPortNumber": 1,
			"comParams": {
				"hardwareHandshake": "None",
				"parity": "None",
				"protocol": "RS232",
				"baudRate": 9600,
				"dataBits": 8,
				"softwareHandshake": "None",
				"stopBits": 1
			}
		},
		"pollIntervalMs": 4500,
		"pollLedTemps": false,
		"coolingTimeMs": 1500,
		"warmingTimeMs": 1500,
		"showVolumeControls": false,
		"volumeUpperLimit": 100,
		"volumeLowerLimit": 0,
		"activeInputs": [
            {
              "key": "hdmi1",
              "name": "HDMI1"
            },
            {
              "key": "hdmi2",
              "name": "HDMI2"
            },
            {
              "key": "displayPort1",
              "name": "Display Port"
            },
            {
              "key": "dvi1",
              "name": "DVI"
            },
            {
              "key": "magicInfo",
              "name": "Magic Info"
            }
          ],
		"friendlyNames": [
			{
				"inputKey": "hdmiIn1",
				"name": "HDMI 1"
			},
			{
				"inputKey": "hdmiIn2",
				"name": "HDMI 2"
			},
			{
				"inputKey": "hdmiIn3",
				"name": "HDMI 3"
			},
			{
				"inputKey": "hdmiIn4",
				"name": "HDMI 4"
			},
			{
				"inputKey": "displayPortIn1",
				"name": "Display Port 1"
			},
			{
				"inputKey": "displayPortIn2",
				"name": "Display Port 2"
			},
			{
				"inputKey": "dviIn",
				"name": "DVI"
			}
		]
	}
},
```

### Display Object using TCP/IP

```JSON
{
	"key": "Display01",
	"uid": 1,
	"name": "Front Display",
	"type": "samsungmdcplugin",
	"group": "display",
	"properties": {
		"id": "01",
		"control": {
			"method": "tcpIp",
			"tcpSshProperties": {
				"address": "10.0.0.200",
				"port": 1515,
				"username": "",
				"password": "",
				"autoReconnect": true,
				"autoReconnectIntervalMs": 10000
			}
		},
		"pollIntervalMs": 4500,
		"pollLedTemps": false,
		"coolingTimeMs": 1500,
		"warmingTimeMs": 1500,
		"showVolumeControls": false,
		"volumeUpperLimit": 100,
		"volumeLowerLimit": 0,
		"activeInputs": [
            {
              "key": "hdmi1",
              "name": "HDMI1"
            },
            {
              "key": "hdmi2",
              "name": "HDMI2"
            },
            {
              "key": "displayPort1",
              "name": "Display Port"
            },
            {
              "key": "dvi1",
              "name": "DVI"
            },
            {
              "key": "magicInfo",
              "name": "Magic Info"
            }
          ],
		"friendlyNames": [
			{
				"inputKey": "hdmiIn1",
				"name": "HDMI 1"
			},
			{
				"inputKey": "hdmiIn2",
				"name": "HDMI 2"
			},
			{
				"inputKey": "hdmiIn3",
				"name": "HDMI 3"
			},
			{
				"inputKey": "hdmiIn4",
				"name": "HDMI 4"
			},
			{
				"inputKey": "displayPortIn1",
				"name": "Display Port 1"
			},
			{
				"inputKey": "displayPortIn2",
				"name": "Display Port 2"
			},
			{
				"inputKey": "dviIn",
				"name": "DVI"
			}
		]
	}
},
```

### Display Plugin Bridge Object

```JSON
{
	"key": "eiscBridge-Displays",
	"uid": 4,
	"name": "eiscBridge Displays",
	"group": "api",
	"type": "eiscApi",
	"properties": {
		"control": {
			"method": "ipidTcp",
			"ipid": "B0",
			"tcpSshProperties": {
				"address": "127.0.0.2",
				"port": 0
			}
		},
		"devices": [
			{
				"deviceKey": "Display01",
				"joinStart": 1
			},
			{
				"deviceKey": "Display02",
				"joinStart": 51
			},
			{
				"deviceKey": "Display03",
				"joinStart": 101
			}
		]
	}
}
```

## Bridge Join Map

- Each display has 50 buttons available
- The I/O number will depend on the joinStart defined in the configuration file
  - Add the defined joinStart to the I/O number if not starting at 1

### Digitals

| Input                          | I/O | Output                     |
| ------------------------------ | --- | -------------------------- |
| Power Off                      | 1   | Power Off Fb               |
| Power On                       | 2   | Power On Fb                |
|                                | 3   | Is Two Display Fb          |
| Volume Up                      | 5   |                            |
| Volume Down                    | 6   |                            |
| Volume Mute Toggle             | 7   | Volume Mute On Fb          |
| Input 1 Select [HDMI 1]        | 11  | Input 1 Fb [HDMI 1]        |
| Input 2 Select [HDMI 2]        | 12  | Input 2 Fb [HDMI 2]        |
| Input 3 Select [HDMI 3]        | 13  | Input 3 Fb [HDMI 3]        |
| Input 4 Select [HDMI 4]        | 14  | Input 4 Fb [HDMI 4]        |
| Input 5 Select [DisplayPort 1] | 15  | Input 5 Fb [DisplayPort 1] |
| Input 6 Select [DisplayPort 2] | 16  | Input 6 Fb [DisplayPort 2] |
| Input 7 Select [DVI 1]         | 17  | Input 7 Fb [DVI 1]         |
| Input 8 Select [FUTURE]        | 18  | Input 8 Fb [FUTURE]        |
| Input 9 Select [FUTURE]        | 19  | Input 9 Fb [FUTURE]        |
| Input 10 Select [FUTURE]       | 20  | Input 10 Fb [FUTURE]       |
|                                | 40  | Button 1 Visibility Fb     |
|                                | 41  | Button 2 Visibility Fb     |
|                                | 42  | Button 3 Visibility Fb     |
|                                | 43  | Button 4 Visibility Fb     |
|                                | 44  | Button 5 Visibility Fb     |
|                                | 45  | Button 6 Visibility Fb     |
|                                | 46  | Button 7 Visibility Fb     |
|                                | 47  | Button 8 Visibility Fb     |
|                                | 48  | Button 9 Visibility Fb     |
|                                | 49  | Button 10 Visibility Fb    |
|                                | 50  | Display Online Fb          |

### Analogs

| Input                      | I/O | Output                     |
| -------------------------- | --- | -------------------------- |
| Volume Level Set           | 5   | Volume Level Fb            |
| Input Number Select [1-10] | 11  | Input Number Fb [1-10]     |
|                            |     | LED Temperature Celsius    |
|                            |     | LED Temperature Fahrenheit |

### Serials

| Input | I/O | Output                       |
| ----- | --- | ---------------------------- |
|       | 1   | Display Name                 |
|       | 11  | Input 1 Name [HDMI 1]        |
|       | 12  | Input 2 Name [HDMI 2]        |
|       | 13  | Input 3 Name [HDMI 3]        |
|       | 14  | Input 4 Name [HDMI 4]        |
|       | 15  | Input 5 Name [DisplayPort 1] |
|       | 16  | Input 6 Name [DisplayPort 2] |
|       | 17  | Input 7 Name [DVI 1]         |
|       | 18  | Input 8 Name [FUTURE]        |
|       | 19  | Input 9 Name [FUTURE]        |
|       | 20  | Input 10 Name [FUTURE]       |
<!-- START Minimum Essentials Framework Versions -->
### Minimum Essentials Framework Versions

- 3.0.0
- 3.0.0
<!-- END Minimum Essentials Framework Versions -->
<!-- START Config Example -->
### Config Example

```json
{
    "key": "GeneratedKey",
    "uid": 1,
    "name": "GeneratedName",
    "type": "SamsungMdcDisplayProperties",
    "group": "Group",
    "properties": {
        "id": "SampleString",
        "volumeUpperLimit": 0,
        "volumeLowerLimit": 0,
        "pollIntervalMs": 0,
        "coolingTimeMs": "SampleValue",
        "warmingTimeMs": "SampleValue",
        "showVolumeControls": true,
        "pollLedTemps": true,
        "friendlyNames": [
            {
                "inputKey": "SampleString",
                "name": "SampleString"
            }
        ],
        "customInputs": [
            {
                "inputCommand": "SampleString",
                "inputConnector": "SampleString",
                "inputIdentifier": "SampleString"
            }
        ],
        "activeInputs": [
            {
                "key": "SampleString",
                "name": "SampleString"
            }
        ]
    }
}
```
<!-- END Config Example -->
<!-- START Supported Types -->

<!-- END Supported Types -->
<!-- START Join Maps -->

<!-- END Join Maps -->
<!-- START Interfaces Implemented -->
### Interfaces Implemented

- IKeyName
- ISelectableItems<byte>
- IBasicVolumeWithFeedback
- ICommunicationMonitor
- IBridgeAdvanced
- IDeviceInfoProvider
- IHasInputs<byte>
- IBasicCommunication
<!-- END Interfaces Implemented -->
<!-- START Base Classes -->
### Base Classes

- DisplayControllerJoinMap
- PepperDash.Essentials.Devices.Common.Displays.TwoWayDisplayBase
- SplusObject
<!-- END Base Classes -->
<!-- START Public Methods -->
### Public Methods

- public void Select()
- public bool IsNewRefusal(byte displayId, byte command, byte? errorCode)
- public void Accepted(byte displayId, byte command)
- public void SetInput(int value)
- public void LinkToApi(
            BasicTriList trilist,
            uint joinStart,
            string joinMapKey,
            EiscApiAdvanced bridge
        )
- public void StatusGet()
- public void PowerGet()
- public void InputHdmi1()
- public void InputHdmi2()
- public void InputHdmi3()
- public void InputHdmi4()
- public void InputDisplayPort1()
- public void InputDisplayPort2()
- public void InputDvi1()
- public void InputMagicInfo()
- public void InputInternalUsb()
- public void InputGet()
- public void InputGeneric(byte data)
- public void SetVolume(ushort level)
- public void VolumeDown(bool pressRelease)
- public void VolumeUp(bool pressRelease)
- public void VolumeGet()
- public void MuteOff()
- public void MuteOn()
- public void MuteToggle()
- public void MuteGet()
- public void TemperatureMaxGet()
- public void LedProductMonitorGet()
- public void UpdateDeviceInfo()
- public void Connect()
- public void Disconnect()
- public void SendText(string text)
- public void SendBytes(byte[] bytes)
- public void Dispatch()
- public void SetPower(bool on)
- public bool SetInput(string value)
- public void SetVolume(int level)
- public void SetMute(bool muted)
- public string Describe()
- public void WAITFORRESPONSE_CallbackFn( object stateInfo )
- public void Close()
- public int Reset()
- public int Set()
- public int Wait( int timeOutInMs )
- public void Close()
- public void ReleaseMutex()
- public int WaitForMutex()
- public int IsNull( object obj )
- public void Assembly_Loads_Successfully()
- public void Assembly_Name_Is_EpiDisplaySamsungMdc()
- public void Factory_Count_Is_Two()
- public void MockSamsungMdcFactory_Exists()
- public void Mock_Console_Help_Fits_The_79_Byte_Limit()
- public void Mock_Factory_Registers_MockSamsungMdc_Type()
- public void SamsungMdcControllerFactory_Exists()
- public void Factory_Has_Parameterless_Constructor()
- public void Factory_Sets_MinimumEssentialsFrameworkVersion_To_3_0_0()
- public void Factory_Sets_TypeNames()
- public void Factory_Source_Contains_TypeName(string typeName)
- public void No_Duplicate_TypeNames_In_Factory_Source()
- public void Config_Class_Exists()
- public void Config_Has_Parameterless_Constructor()
- public void Config_Property_Has_JsonPropertyAttribute(string jsonName)
- public void Nested_Config_Property_Has_JsonPropertyAttribute(string typeName, string jsonName)
- public void Config_Sample_Json_Has_Expected_Keys()
- public void ActiveInputs_Deserialize_As_List_With_Expected_Shape()
- public void Config_Property_Type_Matches(string propertyName, string expectedTypeName)
- public void List_Property_Is_List_Of_Expected_Element(string propertyName, string expectedElementType)
- public void Driver_Frames_Have_Valid_Checksums_And_Lengths()
- public void Spec_Examples_Match_Checksum()
- public void Status_Ack_Is_Spec_Layout()
- public void Bad_Checksum_Is_Ignored()
- public void Other_Display_Id_Is_Ignored()
- public void Broadcast_Set_Applies_Without_Ack_And_Broadcast_Get_Answers()
- public void Unknown_Commands_Are_Silent()
- public void FrameLength_Splits_Back_To_Back_And_Waits_For_Split_Frames()
- public void Off_Panel_Answers_Status_And_Power_And_Naks_The_Rest()
- public void Off_Panel_Answers_Everything_When_NakWhileOff_Is_False()
- public void Power_On_Then_Off_Updates_Driver()
- public void Every_Driver_Input_Round_Trips()
- public void Set_Input_With_Get_Only_Or_Unknown_Code_Is_Nakd(byte code)
- public void Hdmi_Pc_Variants_And_Internal_Usb_Map_To_Their_Ports(string reported, int expectedInput)
- public void SetInput_Accepts_Every_Input_Number()
- public void Status_Poll_While_Off_Leaves_The_Input_Cleared()
- public void Volume_Sweep_Every_Ui_Level(int lower, int upper)
- public void Volume_Feedback_Echoed_Back_Does_Not_Drift(int lower, int upper)
- public void Single_Volume_Tap_Moves_Exactly_One_Step(int lower, int upper)
- public void Tap_Before_Slider_Feedback_Moves_Exactly_One_Step(int lower, int upper)
- public void Volume_Ramp_Reaches_Exactly_The_Configured_Limits(int lower, int upper)
- public void Panel_Level_Above_The_Upper_Limit_Clamps_Feedback()
- public void Volume_Edges()
- public void Mute_On_Off_Round_Trips()
- public void Fault_Ng_Naks_Every_Known_Command_And_Silent_Answers_Nothing()
- public void Nak_Log_Names_The_Refused_Command(byte command, string name)
- public void Nak_Error_Code_Is_The_Byte_Before_The_Checksum()
- public void Refused_Power_Set_Restores_State_And_Requeries_Once(bool panelOn)
- public void Status_Requery_Restores_The_Input_A_Refused_PowerOff_Cleared()
- public void Status_Requery_Leaves_The_Input_Cleared_When_The_Panel_Is_Off()
- public void Refused_Requery_Is_Not_Repeated_And_Keeps_The_Restored_State()
- public void Accepted_Power_Set_Clears_The_Pending_Set()
- public void Repeated_Refusal_Is_Logged_Loudly_Once_Until_Accepted()
- public void Refusals_Are_Tracked_Per_Display_Id()
- public void Nak_Is_Not_Applied_As_A_Value()
- public void Short_Ack_Is_Ignored_Without_Throwing()
- public void Reply_Without_AA_Header_Is_Ignored()
- public void Reply_From_Another_Display_Id_Is_Ignored()
- public void Broadcast_Configured_Driver_Accepts_Any_Display_Id()
- public void Split_Reply_Is_Reassembled_By_The_Driver()
- public void Coalesced_Replies_Are_All_Parsed()
- public void Coalesced_Reply_Followed_By_Partial_Frame_Keeps_The_Partial()
- public void Echoed_Command_Ahead_Of_Reply_Is_Dropped_And_The_Reply_Parsed(bool set)
- public void Bad_Checksum_Reply_Is_Dropped_And_The_Next_Frame_Still_Parsed()
- public void Noise_Before_A_Reply_Is_Skipped()
- public void Led_Monitoring_Temperature_Reaches_The_Driver()
- public void Serial_Number_Reaches_The_Driver()
- public void Software_Version_Reaches_The_Driver(string reported, string expected)
- public void Device_Info_Sends_Only_Spec_Frames_And_Network_Info_Reaches_The_Driver()
- public void Panel_Changes_Run_Through_PanelChange_In_Order()
- public static readonly (string Key, byte Code)
- public void BytesReceived(byte[] eBytes)
<!-- END Public Methods -->
<!-- START Bool Feedbacks -->
### Bool Feedbacks

- MuteFeedback
<!-- END Bool Feedbacks -->
<!-- START Int Feedbacks -->
### Int Feedbacks

- StatusFeedback
- InputNumberFeedback
- CurrentLedTemperatureCelsiusFeedback
- CurrentLedTemperatureFahrenheitFeedback
- VolumeLevelFeedback
<!-- END Int Feedbacks -->
<!-- START String Feedbacks -->

<!-- END String Feedbacks -->
