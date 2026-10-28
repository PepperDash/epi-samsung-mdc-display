using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using PepperDash.Core;

namespace PepperDashPluginSamsungMdcDisplay
{
    /// <summary>
    /// "mocksamsung" console command for driving mock panels by device key
    /// </summary>
    internal static class MockSamsungMdcConsole
    {
        private const string Usage =
            "mocksamsung <deviceKey> status | power on|off | input <name>|<hex code> | volume 0-100 | mute on|off | fault none|ng|silent";

        // The console THROWS on help text over 79 bytes, and inside BuildDevice that makes the type "unknown".
        // Keep this one short line; the full usage prints when the command runs without arguments.
        private const string Help = "Mock Samsung MDC: mocksamsung <key> status|power|input|volume|mute|fault";

        private static readonly Dictionary<string, MockSamsungMdcResponder> Responders =
            new Dictionary<string, MockSamsungMdcResponder>(StringComparer.OrdinalIgnoreCase);

        private static readonly object RegisterLock = new object();
        private static bool commandAdded;

        /// <summary>
        /// Makes a mock panel reachable from the console, adding the command on first use
        /// </summary>
        public static void Register(string deviceKey, MockSamsungMdcResponder responder)
        {
            lock (RegisterLock)
            {
                Responders[deviceKey] = responder;

                if (commandAdded)
                {
                    return;
                }

                // A convenience: never let it stop the display from loading. Only a successful add counts, so
                // the next mock to register retries after a failure instead of leaving the command missing.
                try
                {
                    commandAdded = CrestronConsole.AddNewConsoleCommand(Handle, "mocksamsung", Help, ConsoleAccessLevelEnum.AccessOperator);
                    if (!commandAdded)
                    {
                        Debug.LogError("Unable to add the mocksamsung console command (it may already exist)");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Unable to add the mocksamsung console command: {0}", e.Message);
                }
            }
        }

        private static void Handle(string args)
        {
            var parts = (args ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Mocks can still be registering while the command runs, so read the dictionary under the lock
            MockSamsungMdcResponder responder = null;
            string[] keys;
            lock (RegisterLock)
            {
                if (parts.Length >= 1)
                {
                    Responders.TryGetValue(parts[0], out responder);
                }

                keys = Responders.Keys.ToArray();
            }

            if (parts.Length < 2 || responder == null)
            {
                CrestronConsole.ConsoleCommandResponse("Usage: {0}\r\nInputs: {1}\r\nMock displays: {2}\r\n", Usage,
                    string.Join(", ", MockSamsungMdcResponder.InputCodes.Keys), string.Join(", ", keys));
                return;
            }

            var verb = parts[1].ToLower();
            var rawArg = parts.Length > 2 ? parts[2] : string.Empty;
            var arg = rawArg.ToLower();
            var ok = true;

            switch (verb)
            {
                case "status":
                    break;
                case "power":
                    ok = arg == "on" || arg == "off";
                    if (ok) responder.SetPower(arg == "on");
                    break;
                case "input":
                    ok = responder.SetInput(rawArg);
                    break;
                case "volume":
                    int level;
                    ok = int.TryParse(arg, out level) && level >= 0 && level <= 100;
                    if (ok) responder.SetVolume(level);
                    break;
                case "mute":
                    ok = arg == "on" || arg == "off";
                    if (ok) responder.SetMute(arg == "on");
                    break;
                case "fault":
                    ok = arg == "none" || arg == "ng" || arg == "silent";
                    if (ok)
                        responder.Fault = arg == "ng" ? MockSamsungMdcFault.Ng
                            : arg == "silent" ? MockSamsungMdcFault.Silent
                            : MockSamsungMdcFault.None;
                    break;
                default:
                    ok = false;
                    break;
            }

            if (!ok)
            {
                CrestronConsole.ConsoleCommandResponse("Usage: {0}\r\n", Usage);
                return;
            }

            // status and fault take effect now. The other changes are queued behind commands still being
            // answered, so the state isn't theirs to show yet: acknowledge them instead.
            if (verb == "status" || verb == "fault")
            {
                CrestronConsole.ConsoleCommandResponse("{0}: {1}\r\n", parts[0], responder.Describe());
                return;
            }

            CrestronConsole.ConsoleCommandResponse("{0}: queued {1} {2} (check with: mocksamsung {0} status)\r\n",
                parts[0], verb, rawArg);
        }
    }
}
