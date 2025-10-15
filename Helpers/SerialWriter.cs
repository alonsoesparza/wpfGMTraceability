using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfGMTraceability.Helpers
{
    public class SerialWriter
    {
        private SerialPort _serialPort;
        public SerialWriter(string portName = "COM10", int baudRate = 9600)
        {
            _serialPort = new SerialPort(portName, baudRate)
            {
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                Encoding = System.Text.Encoding.ASCII
            };
        }
        public void OpenPort()
        {
            if (!_serialPort.IsOpen)
            {
                _serialPort.Open();
            }
        }
        public void WriteData(string data)
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.WriteLine(data); 
            }
            else
            {
                throw new InvalidOperationException("Puerto COM no está abierto.");
            }
        }
        public void ClosePort()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }
    }
}
