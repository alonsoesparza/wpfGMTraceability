using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfGMTraceability.Managers
{
    public class SerialPortSession : IDisposable
    {
        private readonly SerialPortManager _manager;
        private object _currentOwner;
        private EventHandler<string> _currentHandler;
        public SerialPortSession(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            _manager = new SerialPortManager(portName, baudRate, parity, dataBits, stopBits);
        }
        public bool IsOpen => _manager.IsOpen;
        public void Open() => _manager.Open();
        public void Close() => _manager.Close();
        public void AssignOwner(object owner, EventHandler<string> handler)
        {
            if (owner == null || handler == null)
                throw new ArgumentNullException("Owner y handler No pueden ser nulos.");

            // Si hay un dueño anterior, lo desconectamos alv
            if (_currentOwner != null && _currentHandler != null)
                _manager.DataReceived -= _currentHandler;

            _currentOwner = owner;
            _currentHandler = handler;
            _manager.DataReceived += _currentHandler;
        }
        public void ReleaseOwner(object owner)
        {
            if (_currentOwner == owner && _currentHandler != null)
            {
                _manager.DataReceived -= _currentHandler;
                _currentOwner = null;
                _currentHandler = null;
            }
        }
        public void Dispose()
        {
            _manager.Dispose();
        }
    }
}
