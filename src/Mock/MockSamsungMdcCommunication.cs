using System;
using System.Collections.Generic;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core.Queues;

namespace PepperDashPluginSamsungMdcDisplay
{
    /// <summary>
    /// In-process stand-in for the display's MDC port. Frames sent by the driver are answered by a
    /// <see cref="MockSamsungMdcResponder"/> on a separate thread, one at a time and in send order,
    /// one receive event per reply frame, like replies from a real port.
    /// </summary>
    /// <remarks>
    /// Not an ISocketStatus. In SamsungMdcDisplayController the only ISocketStatus check copies a
    /// GenericTcpIpClient's hostname into DeviceInfo, so neither choice changes the driver's behaviour;
    /// without it the communication monitor also skips its socket-status handling.
    /// </remarks>
    internal class MockSamsungMdcCommunication : IBasicCommunication
    {
        private static readonly Encoding Latin1 = Encoding.GetEncoding(28591);

        private readonly MockSamsungMdcResponder responder;
        private readonly GenericQueue replyQueue;
        private readonly int responseDelayMs;
        private readonly bool echoCommands;
        private readonly object bufferLock = new object();
        private readonly List<byte> rxBuffer = new List<byte>();

        /// <summary>
        /// Creates a mock communication whose replies come from the given responder
        /// </summary>
        public MockSamsungMdcCommunication(string key, MockSamsungMdcResponder responder, int responseDelayMs, bool echoCommands)
        {
            Key = key;
            this.responder = responder;
            this.responseDelayMs = Math.Max(0, responseDelayMs);
            this.echoCommands = echoCommands;
            replyQueue = new GenericQueue(key + "-replies");

            // Changes made at the panel take their turn behind commands already queued
            responder.PanelChange += change => replyQueue.Enqueue(new ActionMessage(() => Send(null, change())));
        }

        /// <inheritdoc />
        public string Key { get; private set; }

        /// <inheritdoc />
        public event EventHandler<GenericCommMethodReceiveBytesArgs> BytesReceived;

        /// <inheritdoc />
        public event EventHandler<GenericCommMethodReceiveTextArgs> TextReceived;

        /// <inheritdoc />
        public bool IsConnected { get; private set; }

        /// <inheritdoc />
        public void Connect()
        {
            IsConnected = true;
        }

        /// <inheritdoc />
        public void Disconnect()
        {
            IsConnected = false;
        }

        /// <inheritdoc />
        public void SendText(string text)
        {
            if (text == null)
            {
                return;
            }

            SendBytes(Latin1.GetBytes(text));
        }

        /// <inheritdoc />
        public void SendBytes(byte[] bytes)
        {
            if (!IsConnected || bytes == null || bytes.Length == 0)
            {
                return;
            }

            lock (bufferLock)
            {
                rxBuffer.AddRange(bytes);

                int length;
                while ((length = MockSamsungMdcResponder.FrameLength(rxBuffer)) > 0 && rxBuffer.Count >= length)
                {
                    var frame = rxBuffer.GetRange(0, length).ToArray();
                    rxBuffer.RemoveRange(0, length);

                    if (length > 1)
                    {
                        replyQueue.Enqueue(new ActionMessage(() => Send(frame, responder.Respond(frame))));
                    }
                }
            }
        }

        // Runs on the reply queue's thread, in send order
        private void Send(byte[] command, IList<byte[]> frames)
        {
            var echo = echoCommands ? command : null;

            foreach (var frame in frames)
            {
                if (responseDelayMs > 0)
                {
                    CrestronEnvironment.Sleep(responseDelayMs);
                }

                // Checked per frame, after the delay: a fault set mid-reply must stop output at once,
                // or late frames keep the communication monitor online
                if (!IsConnected || responder.Fault == MockSamsungMdcFault.Silent)
                {
                    return;
                }

                var bytes = frame;
                if (echo != null)
                {
                    // Field condition from PR #51: the command comes back ahead of the reply, in one chunk
                    bytes = new byte[echo.Length + frame.Length];
                    echo.CopyTo(bytes, 0);
                    frame.CopyTo(bytes, echo.Length);
                    echo = null;
                }

                Raise(bytes);
            }
        }

        private void Raise(byte[] bytes)
        {
            var bytesHandler = BytesReceived;
            if (bytesHandler != null)
            {
                bytesHandler(this, new GenericCommMethodReceiveBytesArgs(bytes));
            }

            var textHandler = TextReceived;
            if (textHandler != null)
            {
                textHandler(this, new GenericCommMethodReceiveTextArgs(Latin1.GetString(bytes, 0, bytes.Length)));
            }
        }

        private class ActionMessage : IQueueMessage
        {
            private readonly Action action;

            public ActionMessage(Action action)
            {
                this.action = action;
            }

            public void Dispatch()
            {
                action();
            }
        }
    }
}
