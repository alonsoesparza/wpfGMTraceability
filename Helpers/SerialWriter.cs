using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wpfGMTraceability.Models;

namespace wpfGMTraceability.Helpers
{
    public class SerialWriter
    {
        private int _timeoutMs;
        private SerialPort _serialPort;
        public SerialWriter(string portName = "COM9", int baudRate = 9600)
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
        public string ReadData()
        {
            StringBuilder response = new StringBuilder();
            DateTime start = DateTime.Now;
            while ((DateTime.Now - start).TotalMilliseconds < _timeoutMs)
            {
                try
                {
                    string line = _serialPort.ReadLine();
                    response.AppendLine(line);
                    break; 
                }
                catch (TimeoutException)
                {
                    // Silencio, seguimos esperando
                }
            }
            return response.ToString().Trim();
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
