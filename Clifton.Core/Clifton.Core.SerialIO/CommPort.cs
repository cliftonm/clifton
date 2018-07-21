using System;
using System.Collections.Generic;
using System.IO.Ports;

using Clifton.Core.ExtensionMethods;

namespace Clifton.Core.SerialIO
{
    public class CommDataEventArgs : EventArgs
    {
        public char[] Data { get; set; }
    }

    public class CommErrorEventArgs : EventArgs { }

    public class CommPort
    {
        public event EventHandler<CommDataEventArgs> DataReceived;
        public event EventHandler<CommErrorEventArgs> CommError;

        protected SerialPort port;

        public const byte SOH = 0x01;
        public const byte EOT = 0x04;
        public const byte ACK = 0x06;

        public void StartSerialIO(string portName, int baud = 9600, Parity parity = Parity.None, int bits = 8, StopBits stopBits = StopBits.One, Handshake handshake = Handshake.None)
        {
            port = new SerialPort(portName, baud, parity, bits, stopBits);
            port.DataReceived += OnDataReceived;
            port.ErrorReceived += OnErrorReceived;
            port.RtsEnable = true;
            port.DtrEnable = true;
            port.Handshake = handshake;
            port.Open();
        }

        public void Close()
        {
            port.Close();
        }

        public void Write(byte[] data)
        {
            port.Write(data, 0, data.Length);
        }

        protected void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            List<char> data = new List<char>();

            while (port.BytesToRead != 0)
            {
                char c = (char)port.ReadChar();
                data.Add(c);
            }

            DataReceived.Fire(this, new CommDataEventArgs() { Data = data.ToArray() });
        }

        protected void OnErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            CommError.Fire(this, new CommErrorEventArgs());
        }
    }
}
