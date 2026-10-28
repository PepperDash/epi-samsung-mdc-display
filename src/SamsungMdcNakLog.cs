using System.Collections.Generic;

namespace PepperDashPluginSamsungMdcDisplay
{
    /// <summary>
    /// Decides how loudly to log a refused (NAK'd) command. The first refusal of a command is worth seeing;
    /// the same refusal repeated on every poll (e.g. a model without the LED product feature) is not, until
    /// the display accepts that command again.
    /// </summary>
    internal class SamsungMdcNakLog
    {
        private readonly Dictionary<byte, byte?> _refused = new Dictionary<byte, byte?>();

        /// <summary>
        /// Records a refusal; true when it is new (not the same command and error code as the last refusal
        /// since the command was last accepted), so it should be logged at Information rather than Debug
        /// </summary>
        public bool IsNewRefusal(byte command, byte? errorCode)
        {
            byte? last;
            if (_refused.TryGetValue(command, out last) && last == errorCode)
                return false;

            _refused[command] = errorCode;
            return true;
        }

        /// <summary>
        /// The display accepted the command, so its next refusal is new again
        /// </summary>
        public void Accepted(byte command)
        {
            _refused.Remove(command);
        }
    }
}
