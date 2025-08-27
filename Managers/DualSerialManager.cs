using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using wpfGMTraceability.Helpers;

namespace wpfGMTraceability.Managers
{
    public class DualSerialManager
    {
        public SerialPortManager Reader { get; private set; }
        public SerialPortManager Writer { get; private set; }
        public event EventHandler<string> DataReceived;
        public DualSerialManager()
        {
            SerialPortConfig _config;

            var json = File.ReadAllText(App.ConfigPortsFilePath);
            _config = JsonConvert.DeserializeObject<SerialPortConfig>(json);

            Reader = new SerialPortManager(_config.ReadPort, _config.BaudRate, _config.Parity, _config.DataBits, _config.StopBits);
            Writer = new SerialPortManager(_config.WritePort, _config.BaudRate, _config.Parity, _config.DataBits, _config.StopBits);

            Reader.DataReceived += (s, data) => DataReceived?.Invoke(this, data);
        }
        public void Send(string message)
        {
            Writer.Write(message);
            Console.WriteLine($"[TX] {message}");
        }
        public void Start()
        {
            Reader.Open();
            Writer.Open(); 
        }
        public void Stop()
        {
            Reader.Close();
            Writer.Close();
        }
    }
}
