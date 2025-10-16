using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using wpfGMTraceability.Helpers;

namespace wpfGMTraceability.Managers
{
    public class SerialPortManager : IDisposable
    {
        private SerialPort _serialPort;
        private readonly StringBuilder _buffer = new StringBuilder();
        private readonly SynchronizationContext _syncContext;
        private bool _isDisposed;
        public event EventHandler<string> DataReceived;
        public bool IsOpen => _serialPort?.IsOpen ?? false;
        public SerialPortManager(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
            _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
            {
                PortName = portName,
                BaudRate = baudRate,
                Parity = parity,
                DataBits = dataBits,
                StopBits = stopBits,
                Encoding = Encoding.UTF8,
                ReadTimeout = 500,
                WriteTimeout = 500
            };
            _serialPort.DataReceived += OnDataReceived;
        }
        public void Open()
        {
            if (_serialPort != null && !_serialPort.IsOpen)
                _serialPort.Open();
        }
        public void Close()
        {
            if (_serialPort != null && _serialPort.IsOpen)
                _serialPort.Close();
        }
        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                //string data = _serialPort.ReadExisting();

                string buffer = "";
                string linea = "";
                
                bool fContinue = true;
                while (fContinue)
                {
                    buffer += _serialPort.ReadExisting(); // agrega lo nuevo

                    // Si detectas un marcador de fin (por ejemplo '\n')
                    if (buffer.Contains("\n"))
                    {
                        linea = buffer.Substring(0, buffer.IndexOf("\n"));
                        buffer = buffer.Substring(buffer.IndexOf("\n") + 1);
                        fContinue = false;                        
                    }
                }


                //_buffer.Append(linea);

                // Notificar en el hilo de UI
                _syncContext.Post(_ => DataReceived?.Invoke(this, linea), null);

            }
            catch (Exception ex)
            {
                // Agregar a Log de errores
            }
        }
        public void Dispose()
        {
            if (_isDisposed) return;
            try
            {
                Close();
                if (_serialPort != null)
                {
                    _serialPort.DataReceived -= OnDataReceived;
                    _serialPort.Dispose();
                }
            }
            catch { /* Silenciar errores de cierre */ }
            _isDisposed = true;
        }
    }
}
