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
                string data = _serialPort.ReadExisting();
                _buffer.Append(data);

                // Notificar en el hilo de UI
                _syncContext.Post(_ => DataReceived?.Invoke(this, data), null);
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
