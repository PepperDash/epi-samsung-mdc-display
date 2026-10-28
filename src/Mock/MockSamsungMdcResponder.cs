// Also compiled into the tests, which enable nullable reference types; the plugin doesn't
#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PepperDashPluginSamsungMdcDisplay
{
    /// <summary>
    /// How the mock panel misbehaves, for exercising the driver's error paths
    /// </summary>
    internal enum MockSamsungMdcFault
    {
        /// <summary>Answer every command normally</summary>
        None,
        /// <summary>Answer every known command with a NAK ('N')</summary>
        Ng,
        /// <summary>Never answer, so the communication monitor goes offline</summary>
        Silent
    }

    /// <summary>
    /// Answers Samsung MDC frames the way a real panel does, per the MDC protocol spec
    /// (MDC_Protocol_2018 v14.4c, in epi-display-simplexample/_config).
    /// Holds the panel state only and uses no Crestron or PepperDash types, so it can be linked into tests.
    /// </summary>
    /// <remarks>
    /// Command: AA {cmd} {id} {len} {data...} {checksum}
    /// ACK:     AA FF {id} {len} 'A' {r-cmd} {val...} {checksum}
    /// NAK:     AA FF {id} 03 'N' {r-cmd} {err} {checksum}   (04 'N' {r-cmd} {sub} {err} for sub-commands)
    /// The checksum is the low byte of the sum of every byte after the header. The panel is query-only:
    /// it never reports a change unprompted, so panel-side changes show up on the driver's next poll.
    /// </remarks>
    internal class MockSamsungMdcResponder
    {
        public const byte Header = 0xAA;
        public const byte AckNakCommand = 0xFF;
        public const byte Ack = 0x41; // 'A'
        public const byte Nak = 0x4E; // 'N'
        public const byte BroadcastId = 0xFE;

        /// <summary>
        /// Error code sent in NAKs. The spec defines the NAK frame but never lists the error codes.
        /// </summary>
        public const byte NakError = 0x01;

        private const byte CmdStatus = 0x00;
        private const byte CmdSerialNumber = 0x0B;
        private const byte CmdSoftwareVersion = 0x0E;
        private const byte CmdPower = 0x11;
        private const byte CmdVolume = 0x12;
        private const byte CmdMute = 0x13;
        private const byte CmdInput = 0x14;
        private const byte CmdSystemConfiguration = 0x1B;
        private const byte CmdLedProduct = 0xD0;

        private const byte SubNetworkConfiguration = 0x82;
        private const byte SubLedMonitoring = 0x84;

        private static readonly IList<byte[]> Nothing = new byte[0][];

        /// <summary>
        /// Input source codes from spec 2.1.14, by name
        /// </summary>
        public static readonly Dictionary<string, byte> InputCodes = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase)
        {
            { "sVideo", 0x04 }, { "component", 0x08 }, { "av1", 0x0C }, { "av2", 0x0D }, { "scart1", 0x0E },
            { "dvi1", 0x18 }, { "pc", 0x14 }, { "bnc", 0x1E }, { "dviVideo", 0x1F }, { "magicInfo", 0x20 },
            { "hdmi1", 0x21 }, { "hdmi1Pc", 0x22 }, { "hdmi2", 0x23 }, { "hdmi2Pc", 0x24 },
            { "displayPort1", 0x25 }, { "displayPort2", 0x26 }, { "displayPort3", 0x27 }, { "rfTv", 0x30 },
            { "hdmi3", 0x31 }, { "hdmi3Pc", 0x32 }, { "hdmi4", 0x33 }, { "hdmi4Pc", 0x34 }, { "tv", 0x40 },
            { "pluginModule", 0x50 }, { "hdBaseT", 0x55 }, { "ocm", 0x56 }, { "mediaMagicInfoS", 0x60 },
            { "screenMirroring", 0x61 }, { "internalUsb", 0x62 }, { "urlLauncher", 0x63 }, { "iwb", 0x64 },
            { "webBrowser", 0x65 }, { "remoteWorkspace", 0x66 }
        };

        /// <summary>
        /// "Get only" input codes (spec 2.1.14 note): the panel reports them but refuses them in a set
        /// </summary>
        private static readonly HashSet<byte> GetOnlyInputCodes = new HashSet<byte> { 0x1F, 0x22, 0x24, 0x32, 0x34 };

        private readonly object stateLock = new object();
        private readonly bool nakWhileOff;
        private readonly int temperatureC;
        private readonly string serialNumber;
        private readonly string softwareVersion;
        private readonly byte[] ipAddress;

        private bool powerIsOn;
        private byte input;
        private int volume;
        private bool isMuted;
        private byte aspect;

        // Written by the console thread, read by the reply thread: synchronize both sides
        private MockSamsungMdcFault fault;

        /// <summary>
        /// Creates a responder for a panel with the given ID and starting state
        /// </summary>
        public MockSamsungMdcResponder(byte displayId, MockSamsungMdcOptions options)
        {
            options = options ?? new MockSamsungMdcOptions();

            DisplayId = displayId;
            nakWhileOff = options.NakWhileOff;
            powerIsOn = options.PowerOn;
            byte code;
            input = TryParseInput(options.Input, out code) ? code : (byte)0x21;
            volume = Math.Max(0, Math.Min(100, options.Volume));
            isMuted = options.Muted;
            aspect = 0x10; // PC 16:9
            temperatureC = Math.Max(0, Math.Min(254, options.TemperatureC));
            serialNumber = options.SerialNumber ?? string.Empty;
            softwareVersion = options.SoftwareVersion ?? string.Empty;
            ipAddress = ParseIp(options.IpAddress);
        }

        /// <summary>
        /// Raised with a change made at the panel (from the console). The communication runs it on its
        /// reply thread, in order with protocol commands, and sends the frames it returns (always none:
        /// an MDC panel reports nothing unprompted). With no subscriber it runs at once.
        /// </summary>
        public event Action<Func<IList<byte[]>>> PanelChange;

        /// <summary>
        /// The panel's MDC ID
        /// </summary>
        public byte DisplayId { get; private set; }

        /// <summary>
        /// Current fault mode
        /// </summary>
        public MockSamsungMdcFault Fault
        {
            get { lock (stateLock) return fault; }
            set { lock (stateLock) fault = value; }
        }

        /// <summary>
        /// Number of bytes in the frame that starts at buffer[0], or 0 if it can't be known yet.
        /// Returns 1 for a byte that isn't a header, so the caller drops it.
        /// </summary>
        public static int FrameLength(IList<byte> buffer)
        {
            if (buffer.Count == 0) return 0;
            if (buffer[0] != Header) return 1;
            return buffer.Count >= 4 ? 5 + buffer[3] : 0; // header, cmd, id, len, data[len], checksum
        }

        /// <summary>
        /// Low byte of the sum of bytes [1, count) - every byte after the header
        /// </summary>
        public static byte Checksum(IList<byte> frame, int count)
        {
            var sum = 0;
            for (var i = 1; i < count; i++) sum += frame[i];
            return (byte)(sum & 0xFF);
        }

        /// <summary>
        /// Answers one complete command frame
        /// </summary>
        /// <returns>The reply frames in order; empty when the panel stays silent</returns>
        public IList<byte[]> Respond(byte[] frame)
        {
            if (frame == null || frame.Length < 5 || frame[0] != Header || frame.Length != 5 + frame[3])
            {
                return Nothing;
            }

            lock (stateLock)
            {
                if (fault == MockSamsungMdcFault.Silent)
                {
                    return Nothing;
                }

                // The spec defines no reply to a bad checksum; a frame the panel can't trust is ignored
                if (Checksum(frame, frame.Length - 1) != frame[frame.Length - 1])
                {
                    return Nothing;
                }

                // Another display's ID: not for this panel
                var id = frame[2];
                var broadcast = id == BroadcastId;
                if (id != DisplayId && !broadcast)
                {
                    return Nothing;
                }

                var command = frame[1];
                var data = new byte[frame[3]];
                Array.Copy(frame, 4, data, 0, data.Length);

                // Unknown commands get no reply, so gaps in the mock show up as unanswered commands
                if (!IsKnown(command))
                {
                    return Nothing;
                }

                byte[] reply;
                if (fault == MockSamsungMdcFault.Ng)
                {
                    reply = NakFor(command, data);
                }
                else if (!powerIsOn && nakWhileOff && command != CmdStatus && command != CmdPower)
                {
                    reply = NakFor(command, data);
                }
                else
                {
                    reply = Handle(command, data);
                }

                // Spec 1.2.1: with ID 0xFE every panel applies a SET but none sends an ACK. The spec
                // is silent on GETs to 0xFE; this mock answers them.
                if (broadcast && IsSet(command, data))
                {
                    return Nothing;
                }

                return reply == null ? Nothing : new[] { reply };
            }
        }

        /// <summary>
        /// Builds a command frame with the given ID and checksum, the way the driver does
        /// </summary>
        public static byte[] Command(byte command, byte id, params byte[] data)
        {
            var frame = new byte[5 + data.Length];
            frame[0] = Header;
            frame[1] = command;
            frame[2] = id;
            frame[3] = (byte)data.Length;
            data.CopyTo(frame, 4);
            frame[frame.Length - 1] = Checksum(frame, frame.Length - 1);
            return frame;
        }

        /// <summary>
        /// Resolves an input name ("hdmi1") or hex code ("0x21", "21") to its MDC code
        /// </summary>
        public static bool TryParseInput(string value, out byte code)
        {
            code = 0;
            if (string.IsNullOrEmpty(value)) return false;
            if (InputCodes.TryGetValue(value, out code)) return true;

            var hex = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value.Substring(2) : value;
            return byte.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code);
        }

        // Panel-side changes. They never mutate state directly: a driver command still waiting in the reply
        // queue would apply after the change and the two would disagree. Queue the change instead.

        public void SetPower(bool on)
        {
            QueuePanelChange(() => powerIsOn = on);
        }

        /// <returns>False if the input isn't a known name or a hex code</returns>
        public bool SetInput(string value)
        {
            byte code;
            if (!TryParseInput(value, out code)) return false;
            QueuePanelChange(() => input = code);
            return true;
        }

        public void SetVolume(int level)
        {
            var clamped = Math.Max(0, Math.Min(100, level));
            QueuePanelChange(() => volume = clamped);
        }

        public void SetMute(bool muted)
        {
            QueuePanelChange(() => isMuted = muted);
        }

        /// <summary>
        /// One-line description of the panel state, for the console
        /// </summary>
        public string Describe()
        {
            lock (stateLock)
            {
                var name = InputCodes.Where(i => i.Value == input).Select(i => i.Key).FirstOrDefault() ?? "unknown";
                return string.Format("id=0x{0:X2} power={1} input={2} (0x{3:X2}) volume={4} mute={5} fault={6}",
                    DisplayId, powerIsOn ? "on" : "off", name, input, volume, isMuted ? "on" : "off", fault);
            }
        }

        private void QueuePanelChange(Action change)
        {
            Func<IList<byte[]>> locked = () =>
            {
                lock (stateLock) change();
                return Nothing;
            };

            var handler = PanelChange;
            if (handler != null)
            {
                handler(locked);
            }
            else
            {
                locked();
            }
        }

        private static bool IsKnown(byte command)
        {
            switch (command)
            {
                case CmdStatus:
                case CmdSerialNumber:
                case CmdSoftwareVersion:
                case CmdPower:
                case CmdVolume:
                case CmdMute:
                case CmdInput:
                case CmdSystemConfiguration:
                case CmdLedProduct:
                    return true;
                default:
                    return false;
            }
        }

        // A SET carries data; the sub-command families (0x1B, 0xD0) carry the sub-command as data even for a GET
        private static bool IsSet(byte command, byte[] data)
        {
            if (command == CmdSystemConfiguration || command == CmdLedProduct) return data.Length > 1;
            return data.Length > 0;
        }

        // Returns the reply frame, or null for silence
        private byte[] Handle(byte command, byte[] data)
        {
            switch (command)
            {
                case CmdStatus:
                    if (data.Length != 0) return NakFor(command, data);
                    // Ack len 0x09: 'A' 00 power volume mute input aspect NTimeNF FTimeNF
                    return AckFor(command, (byte)(powerIsOn ? 1 : 0), (byte)volume, (byte)(isMuted ? 1 : 0),
                        input, aspect, 0x00, 0x00);

                case CmdPower:
                    if (data.Length == 0) return AckFor(command, (byte)(powerIsOn ? 1 : 0));
                    if (data.Length != 1 || data[0] > 0x02) return NakFor(command, data);
                    // 0x02 = reboot: the panel comes back on
                    powerIsOn = data[0] != 0x00;
                    return AckFor(command, data[0]);

                case CmdVolume:
                    if (data.Length == 0) return AckFor(command, (byte)volume);
                    if (data.Length != 1 || data[0] > 100) return NakFor(command, data);
                    volume = data[0];
                    return AckFor(command, (byte)volume);

                case CmdMute:
                    if (data.Length == 0) return AckFor(command, (byte)(isMuted ? 1 : 0));
                    if (data.Length != 1 || data[0] > 0x01) return NakFor(command, data);
                    isMuted = data[0] == 0x01;
                    return AckFor(command, data[0]);

                case CmdInput:
                    if (data.Length == 0) return AckFor(command, input);
                    if (data.Length != 1 || !InputCodes.ContainsValue(data[0]) || GetOnlyInputCodes.Contains(data[0]))
                        return NakFor(command, data);
                    input = data[0];
                    return AckFor(command, input);

                case CmdSerialNumber:
                    if (data.Length != 0) return NakFor(command, data);
                    // Ack len 0x14: 'A' 0B, then 18 bytes: serial number (15) + reserved (3)
                    return AckFor(command, Padded(serialNumber, 18, 15));

                case CmdSoftwareVersion:
                    if (data.Length != 0) return NakFor(command, data);
                    // Ack len 2 + N (max 0x34): 12 bytes project info, then the software version
                    return AckFor(command, Padded(softwareVersion, Math.Min(50, softwareVersion.Length), 50));

                case CmdSystemConfiguration:
                    if (data.Length == 1 && data[0] == SubNetworkConfiguration)
                    {
                        // Ack len 0x13: 'A' 1B 82, then IP, subnet, gateway, DNS (4 bytes each)
                        var values = new List<byte> { SubNetworkConfiguration };
                        values.AddRange(ipAddress);
                        values.AddRange(new byte[] { 255, 255, 255, 0 });
                        values.AddRange(new byte[] { ipAddress[0], ipAddress[1], ipAddress[2], 1 });
                        values.AddRange(new byte[] { ipAddress[0], ipAddress[1], ipAddress[2], 1 });
                        return AckFor(command, values.ToArray());
                    }
                    return NakFor(command, data);

                case CmdLedProduct:
                    if (data.Length == 1 && data[0] == SubLedMonitoring)
                    {
                        // 'A' D0 84 Power&IC HDBT Temperature Illuminance (no module data; all optional)
                        return AckFor(command, SubLedMonitoring, 0xFF, 0x00, (byte)temperatureC, 0xFF);
                    }
                    return NakFor(command, data);

                default:
                    return null;
            }
        }

        private byte[] AckFor(byte command, params byte[] values)
        {
            var frame = new List<byte> { Header, AckNakCommand, DisplayId, (byte)(values.Length + 2), Ack, command };
            frame.AddRange(values);
            frame.Add(0x00);
            var result = frame.ToArray();
            result[result.Length - 1] = Checksum(result, result.Length - 1);
            return result;
        }

        // Sub-command families NAK with the sub-command echoed: AA FF id 04 'N' r-cmd sub err cs
        private byte[] NakFor(byte command, byte[] data)
        {
            var subCommand = (command == CmdSystemConfiguration || command == CmdLedProduct) && data.Length > 0;
            var frame = subCommand
                ? new byte[] { Header, AckNakCommand, DisplayId, 0x04, Nak, command, data[0], NakError, 0x00 }
                : new byte[] { Header, AckNakCommand, DisplayId, 0x03, Nak, command, NakError, 0x00 };
            frame[frame.Length - 1] = Checksum(frame, frame.Length - 1);
            return frame;
        }

        private static byte[] Padded(string text, int length, int maxText)
        {
            var result = new byte[length];
            var bytes = Encoding.ASCII.GetBytes(text.Length > maxText ? text.Substring(0, maxText) : text);
            Array.Copy(bytes, result, Math.Min(bytes.Length, length));
            return result;
        }

        private static byte[] ParseIp(string ip)
        {
            var parts = (ip ?? string.Empty).Split('.');
            var result = new byte[] { 192, 168, 0, 10 };
            if (parts.Length != 4) return result;

            var parsed = new byte[4];
            for (var i = 0; i < 4; i++)
            {
                if (!byte.TryParse(parts[i], out parsed[i])) return result;
            }
            return parsed;
        }
    }
}
