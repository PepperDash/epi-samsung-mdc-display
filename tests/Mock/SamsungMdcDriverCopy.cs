namespace PepperDashPluginSamsungMdcDisplay.Tests.Mock;

/// <summary>
/// The driver's own wire formatting and reply parsing, copied from src/SamsungMdc.cs (feature/migrate-to-v3-essentials
/// at 52cccd8) with feedback objects replaced by plain fields. Keep the copied lines verbatim: the point is to run
/// the mock against exactly what the driver sends and how it reads replies. Re-copy when SamsungMdc.cs changes.
/// </summary>
internal sealed class SamsungMdcDriverCopy
{
    public const byte Header = 0xAA;
    public const byte StatusControl = 0x00;
    public const byte PowerControl = 0x11;
    public const byte VolumeControl = 0x12;
    public const byte MuteControl = 0x13;
    public const byte InputSourceControl = 0x14;
    public const byte LedProductFeature = 0xD0;
    public const byte LedSubcmdMonitoring = 0x84;
    public const byte SerialNumberControl = 0x0B;
    public const byte SwVersionControl = 0x0E;
    public const byte SystemConfiguration = 0x1B;
    public const byte SystemConfigurationMacAddress = 0x81;
    public const byte NetworkConfiguration = 0x82;

    /// <summary>
    /// Default routing ports (no customInputs), in InitInputPortsAndFeedbacks order: key and FeedbackMatchObject.
    /// "usb" has a null selector.
    /// </summary>
    public static readonly (string Key, byte Code)[] DefaultPorts =
    {
        ("hdmiIn1", 0x21), ("hdmiIn2", 0x23), ("hdmiIn3", 0x31), ("hdmiIn4", 0x33),
        ("displayPortIn1", 0x25), ("displayPortIn2", 0x26), ("dviIn", 0x18), ("mediaPlayer", 0x20), ("usb", 0x99),
    };

    private readonly int _lowerLimit;
    private readonly int _upperLimit;
    private bool ScaleVolume { get; set; }
    private byte[] _incomingBuffer = { };

    public SamsungMdcDriverCopy(byte id, int volumeLowerLimit, int volumeUpperLimit)
    {
        Id = id;
        _lowerLimit = volumeLowerLimit;
        _upperLimit = volumeUpperLimit;

        // InitVolumeControls
        if (_upperLimit != _lowerLimit && _upperLimit > _lowerLimit)
        {
            ScaleVolume = true;
        }
    }

    public byte Id { get; }

    // Feedback state the parser drives
    public bool PowerIsOn;
    public ushort VolumeLevelForSig;
    public bool IsMuted;
    public int CurrentInputNumber;
    public int LedTemperatureCelsius;
    public string? SerialNumber;
    public string? FirmwareVersion;
    public string? IpAddress;
    public readonly List<Exception> Exceptions = new();
    public readonly List<byte[]> ParsedMessages = new();

    // ---- Sending ----

    /// <summary>SendBytes: sets the ID and checksum (the volume pacing sleep is left out)</summary>
    public byte[] Format(byte[] b)
    {
        b[2] = Id;
        // append checksum by adding all bytes, except last which should be 00
        var checksum = 0;
        for (var i = 1; i < b.Length - 1; i++) // add 2nd through 2nd-to-last bytes
        {
            checksum += b[i];
        }
        checksum = checksum & 0x000000FF; // mask off MSBs
        b[b.Length - 1] = (byte)checksum;
        return b;
    }

    public byte[] StatusGet() => Format(new byte[] { Header, StatusControl, 0x00, 0x00, 0x00 });
    public byte[] LedProductMonitorGet() => Format(new byte[] { Header, LedProductFeature, 0x00, 0x01, LedSubcmdMonitoring, 0x00 });
    public byte[] PowerOn() => Format(new byte[] { Header, PowerControl, 0x00, 0x01, 0x01, 0x00 });
    public byte[] PowerOff() => Format(new byte[] { Header, PowerControl, 0x00, 0x01, 0x00, 0x00 });
    public byte[] PowerGet() => Format(new byte[] { Header, PowerControl, 0x00, 0x00, 0x00 });
    public byte[] InputGeneric(byte data) => Format(new byte[] { Header, InputSourceControl, 0x00, 0x01, data, 0x00 });
    public byte[] InputGet() => Format(new byte[] { Header, InputSourceControl, 0x00, 0x00, 0x00 });
    public byte[] VolumeGet() => Format(new byte[] { Header, VolumeControl, 0x00, 0x00, 0x00 });
    public byte[] MuteOn() => Format(new byte[] { Header, MuteControl, 0x00, 0x01, 0x01, 0x00 });
    public byte[] MuteOff() => Format(new byte[] { Header, MuteControl, 0x00, 0x01, 0x00, 0x00 });
    public byte[] MuteGet() => Format(new byte[] { Header, MuteControl, 0x00, 0x00, 0x00 });

    /// <summary>UpdateDeviceInfo: the four frames it sends, in order</summary>
    public byte[][] UpdateDeviceInfo() => new[]
    {
        Format(new byte[] { Header, SerialNumberControl, 0x00, 0x00, 0x00 }),
        Format(new byte[] { Header, SwVersionControl, 0x00, 0x00, 0x00 }),
        Format(new byte[] { Header, NetworkConfiguration, 0x00, 0x00, 0x00 }),
        Format(new byte[] { Header, SystemConfigurationMacAddress, 0x00, 0x00, 0x00 }),
    };

    /// <summary>SendVolumeCommand: the frame for a 0-65535 level</summary>
    public byte[] SendVolumeCommand(ushort level)
    {
        int scaled;
        if (!ScaleVolume)
        {
            scaled = (int)Scale(level, 0, 65535, 0, 100);
        }
        else
        {
            scaled = (int)Scale(level, 0, 65535, _lowerLimit, _upperLimit);
        }

        return Format(new byte[] { Header, VolumeControl, 0x00, 0x01, Convert.ToByte(scaled), 0x00 });
    }

    /// <summary>SetInput(int) guard: the 1-based input numbers it accepts</summary>
    public static bool SetInputAccepts(int value, int inputPortCount) => !(value <= 0 || value >= inputPortCount);

    // ---- Receiving ----

    /// <summary>Communication_BytesReceived</summary>
    public void BytesReceived(byte[] eBytes)
    {
        try
        {
            // Append the incoming bytes with whatever is in the buffer
            var newBytes = new byte[_incomingBuffer.Length + eBytes.Length];
            _incomingBuffer.CopyTo(newBytes, 0);
            eBytes.CopyTo(newBytes, _incomingBuffer.Length);

            // Get data length
            if (newBytes.Length >= 6)
            {
                // check for header

                // header + length + checksum
                var dataLength = 5 + newBytes[3];
                if (newBytes.Length >= dataLength)
                {
                    var message = new byte[dataLength];
                    Array.Copy(newBytes, 0, message, 0, dataLength);
                    ParseMessage(message);
                    byte[] clear = { };
                    _incomingBuffer = clear;
                    return;
                }
            }
            if (newBytes[0] == 0xAA)
            {
                _incomingBuffer = newBytes;
            }
            else
            {
                byte[] clear = { };
                _incomingBuffer = clear;
            }
        }
        catch (Exception ex)
        {
            Exceptions.Add(ex);
        }
    }

    /// <summary>ParseMessage, same indices</summary>
    private void ParseMessage(byte[] message)
    {
        ParsedMessages.Add(message);
        var command = message[5];

        switch (command)
        {
            case StatusControl:
                UpdatePowerFb(message[6]);
                UpdateVolumeFb(message[7]);
                UpdateMuteFb(message[8]);
                UpdateInputFb(message[9]);
                break;
            case PowerControl:
                UpdatePowerFb(message[6]);
                break;
            case VolumeControl:
                UpdateVolumeFb(message[6]);
                break;
            case MuteControl:
                UpdateMuteFb(message[6]);
                break;
            case InputSourceControl:
                UpdateInputFb(message[6]);
                break;
            case LedProductFeature:
                if (message[6] == LedSubcmdMonitoring)
                {
                    LedTemperatureCelsius = Convert.ToInt16(message[9]);
                }
                break;
            case SerialNumberControl:
                {
                    var serialNumber = new byte[18];
                    Array.Copy(message, 6, serialNumber, 0, 18);
                    SerialNumber = System.Text.Encoding.UTF8.GetString(serialNumber, 0, serialNumber.Length);
                    break;
                }
            case SwVersionControl:
                {
                    var length = message[3];
                    var firmware = new byte[length];
                    Array.Copy(message, 6, firmware, 0, length);
                    FirmwareVersion = System.Text.Encoding.UTF8.GetString(firmware, 0, 12);
                    break;
                }
            case SystemConfiguration:
                {
                    var length = message[3];
                    if (message[4] == NetworkConfiguration)
                    {
                        var ipAddressInfo = new byte[length - 1];
                        Array.Copy(message, 7, ipAddressInfo, 0, length - 1);
                        IpAddress = string.Format("{0}.{1}.{2}.{3}", ipAddressInfo[0], ipAddressInfo[1], ipAddressInfo[2], ipAddressInfo[3]);
                        break;
                    }
                    break;
                }
        }
    }

    private void UpdatePowerFb(byte powerByte)
    {
        var newVal = powerByte == 1;
        if (!newVal)
        {
            CurrentInputNumber = 0;
        }
        PowerIsOn = newVal;
    }

    private void UpdateVolumeFb(byte b)
    {
        ushort newVol;
        if (!ScaleVolume)
        {
            newVol = (ushort)Scale(b, 0, 100, 0, 65535);
        }
        else
        {
            newVol = (ushort)Scale(b, _lowerLimit, _upperLimit, 0, 65535);
        }
        VolumeLevelForSig = newVol;
    }

    private void UpdateMuteFb(byte b)
    {
        IsMuted = b == 1;
    }

    private void UpdateInputFb(byte b)
    {
        var index = Array.FindIndex(DefaultPorts, p => p.Code.Equals(b));
        if (index < 0)
            return;
        CurrentInputNumber = index + 1;
    }

    /// <summary>PepperDash.Essentials.Core.NumericalHelpers.Scale</summary>
    public static double Scale(double input, double inMin, double inMax, double outMin, double outMax)
    {
        double inputRange = inMax - inMin;

        if (inputRange <= 0)
        {
            throw new ArithmeticException(string.Format("Invalid Input Range '{0}' for Scaling.  Min '{1}' Max '{2}'.", inputRange, inMin, inMax));
        }

        double outputRange = outMax - outMin;

        var output = (((input - inMin) * outputRange) / inputRange) + outMin;

        return output;
    }
}
