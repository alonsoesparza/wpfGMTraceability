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
    public class SerialPortManager
    {
        private SerialPort _port;
        private SerialPortConfig _config;
        public event EventHandler<string> DataReceived;

        private Timer _reconnectTimer;
        private readonly TimeSpan _reconnectInterval = TimeSpan.FromSeconds(5);

        public SerialPortManager(string portName, int baudRate, System.IO.Ports.Parity parity, int dataBits, System.IO.Ports.StopBits stopBits)
        {
            if (string.IsNullOrWhiteSpace(portName))
            {
                SettingsManager.SerialPortStatusMessage = $"Error al abrir el puerto";
                return;
            }
            _port = new SerialPort
            {
                PortName = portName,
                BaudRate = baudRate,
                Parity = parity,
                DataBits = dataBits,
                StopBits = stopBits
            };
            _port.DataReceived += (s, e) =>
            {
                string data = _port.ReadExisting();
                DataReceived?.Invoke(this, data);
            };
            _port.ErrorReceived += OnErrorReceived;
        }
        public void Open()
        {
            try
            {
                if (_port != null) { _port.Open(); } else {
                    SettingsManager.SerialPortStatusMessage = $"Error al abrir el puerto: Configuración inválida.";
                    return;
                }

                SettingsManager.SerialPortStatusMessage = $"Puerto {_port.PortName} abierto correctamente.";
            }
            catch (System.IO.IOException ex)
            {
                SettingsManager.SerialPortStatusMessage = $"Error al abrir el puerto: {ex.Message}";
                //En caso que se requiera iniciar una reconexion automática
                //StartReconnectTimer();
            }
        }
        public void Close()
        {
            _reconnectTimer?.Dispose();

            if (_port != null)
            {
                _port.DataReceived -= OnDataReceived;
                _port.ErrorReceived -= OnErrorReceived;

                if (_port.IsOpen) { _port.Close(); }
                    
            }
        }
        public void Write(string data)
        {
            if (_port?.IsOpen == true) { _port.WriteLine(data); }
        }
        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = _port.ReadExisting();
                DataReceived?.Invoke(this, data);
            }
            catch (Exception ex)
            {
                SettingsManager.SerialPortStatusMessage = $"Error: {ex.Message}";
            }
        }
        private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            Debug.WriteLine($"Error de puerto: {e.EventType}");
            //RestartConnection();
        }
        private void StartReconnectTimer()
        {
            _reconnectTimer?.Dispose();
            _reconnectTimer = new Timer(_ =>
            {
                Debug.WriteLine("Intentando reconectar...");
                RestartConnection();
            }, null, _reconnectInterval, _reconnectInterval);
        }
        private void RestartConnection()
        {
            try
            {
                if (_port?.IsOpen == true)
                {
                    _port.Close();
                }

                Open();

                if (_port?.IsOpen == true)
                {
                    _reconnectTimer?.Dispose();
                    SettingsManager.SerialPortStatusMessage = "Reconexion exitosa.";
                }
            }
            catch (Exception ex)
            {
                SettingsManager.SerialPortStatusMessage = $"Fallo la reconexión: {ex.Message}";
                // El timer jala de nuevo
            }
        }
    }
}
