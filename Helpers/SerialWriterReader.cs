using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace wpfGMTraceability.Helpers
{
    public class SerialWriterReader
    {
        private SerialPort _serialPort;

        public SerialWriterReader(string puerto, int baudRate = 9600, int timeout = 1000)
        {
            _serialPort = new SerialPort(puerto, baudRate);
            _serialPort.ReadTimeout = timeout;
            _serialPort.WriteTimeout = timeout;
        }

        public void OpenPort()
        {
            try
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                    Console.WriteLine($"Conectado a {_serialPort.PortName} a {_serialPort.BaudRate} baudios.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al conectar: {ex.Message}");
            }
        }

        public void Write(string data)
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.WriteLine(data);
                Console.WriteLine($"Enviado: {data}");
            }
            else
            {
                Console.WriteLine("Puerto no conectado.");
            }
        }

        public string Read()
        {
            try
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.ReadTimeout = -1;
                    string respuesta = _serialPort.ReadLine();
                    Console.WriteLine($"Recibido: {respuesta}");
                    return respuesta;
                }
                else
                {
                    Console.WriteLine("Puerto no conectado.");
                    return null;
                }
            }
            catch (TimeoutException)
            {
                Console.WriteLine("Tiempo de espera agotado.");
                return null;
            }
        }

        public string WriteAndRead(string mensaje)
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.DiscardInBuffer(); // Limpia buffer antes de enviar
                _serialPort.WriteLine(mensaje);
                Console.WriteLine($"📤 Enviado: {mensaje}");

                //Thread.Sleep(esperaMs); // Espera antes de leer

                string respuesta = Read();
                return respuesta;
                Console.WriteLine($"📥 Respuesta: {respuesta}");
            }
            else
            {
                return null;
                Console.WriteLine("⚠️ Puerto no conectado.");
            }
        }

        public void ClosePort()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
                Console.WriteLine("Puerto cerrado.");
            }
        }
    }
}