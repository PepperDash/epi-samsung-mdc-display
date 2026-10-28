using System.Collections.Generic;

namespace PepperDashPluginSamsungMdcDisplay
{
    /// <summary>
    /// Decides how loudly to log a refused (NAK'd) command. The first refusal of a command is worth seeing;
    /// the same refusal repeated on every poll (e.g. a model without the LED product feature) is not, until
    /// the display accepts that command again. Tracked per display ID, since a driver configured with the
    /// broadcast ID hears every display on the line.
    /// </summary>
    internal class SamsungMdcNakLog
    {
        // (display ID, command) -> every error code refused since the display last accepted the command
        private readonly Dictionary<int, HashSet<int>> _refused = new Dictionary<int, HashSet<int>>();

        /// <summary>
        /// Records a refusal; true when this display has not refused this command with this error code since
        /// it last accepted the command, so it should be logged at Information rather than Debug
        /// </summary>
        public bool IsNewRefusal(byte displayId, byte command, byte? errorCode)
        {
            var key = Key(displayId, command);
            HashSet<int> codes;
            if (!_refused.TryGetValue(key, out codes))
            {
                codes = new HashSet<int>();
                _refused[key] = codes;
            }

            // -1 stands for a NAK without an error byte
            return codes.Add(errorCode.HasValue ? errorCode.Value : -1);
        }

        /// <summary>
        /// The display accepted the command, so its next refusal is new again
        /// </summary>
        public void Accepted(byte displayId, byte command)
        {
            _refused.Remove(Key(displayId, command));
        }

        private static int Key(byte displayId, byte command)
        {
            return (displayId << 8) | command;
        }
    }
}
