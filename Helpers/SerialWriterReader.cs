using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace wpfGMTraceability.Helpers
{
    public class SerialWriterReader: IDisposable
    {
        private readonly SerialPort _serialPort;
        private readonly object _gate = new object();
        private bool _disposed;
        private int _defaultTimeoutMs;

        public SerialWriterReader(string puerto, int baudRate = 9600, int timeout = 1000)
        {
            _defaultTimeoutMs = timeout;

            _serialPort = new SerialPort(puerto, baudRate)
            {
                ReadTimeout = timeout,           // timeout por defecto
                WriteTimeout = timeout,
                Handshake = Handshake.None,
                DtrEnable = true,                // Arduino suele resetear con DTR
                RtsEnable = false,
                NewLine = "\n",                  // .ReadLine() cortará en '\n'
                Encoding = System.Text.Encoding.ASCII
            };
        }

        public void OpenPort()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SerialWriterReader));
            if (_serialPort.IsOpen) return;

            _serialPort.Open();

            // Dar tiempo a que Arduino reinicie y limpie buffers
            Thread.Sleep(1500);
            try
            {
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();
            }
            catch { /* ignore */ }
        }

        public void ClosePort()
        {
            if (_serialPort.IsOpen) _serialPort.Close();
        }

        public void SetDefaultTimeout(int timeoutMs)
        {
            _defaultTimeoutMs = timeoutMs;
            if (_serialPort.IsOpen)
            {
                lock (_gate)
                {
                    _serialPort.ReadTimeout = timeoutMs;
                    _serialPort.WriteTimeout = timeoutMs;
                }
            }
        }

        public void Write(string data)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SerialWriterReader));
            if (!_serialPort.IsOpen) return;

            lock (_gate)
            {
                _serialPort.WriteLine(data);
            }
        }

        public string Read(int? timeoutMs = null)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SerialWriterReader));
            if (!_serialPort.IsOpen) return string.Empty;

            lock (_gate)
            {
                _serialPort.ReadTimeout = timeoutMs ?? _defaultTimeoutMs;
                try
                {
                    var line = _serialPort.ReadLine();
                    return line ?? string.Empty;
                }
                catch (TimeoutException)
                {
                    return string.Empty;
                }
            }
        }

        // --- ASYNC ---

        public Task WriteAsync(string data)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SerialWriterReader));
            return Task.Run(() =>
            {
                if (!_serialPort.IsOpen) return;
                lock (_gate)
                {
                    _serialPort.WriteLine(data);
                }
            });
        }

        public Task<string> ReadAsync(int? timeoutMs = null)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SerialWriterReader));
            return Task.Run(() =>
            {
                if (!_serialPort.IsOpen) return string.Empty;
                lock (_gate)
                {
                    _serialPort.ReadTimeout = timeoutMs ?? _defaultTimeoutMs;
                    try
                    {
                        var s = _serialPort.ReadLine();
                        return s ?? string.Empty;
                    }
                    catch (TimeoutException)
                    {
                        return string.Empty;
                    }
                }
            });
        }

        /// <summary>
        /// Envía y espera UNA línea de respuesta con timeout por llamada (no cuelga la UI).
        /// </summary>
        public Task<string> WriteAndReadAsync(string mensaje, int? timeoutMs = null, bool discardInBuffer = true)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SerialWriterReader));
            return Task.Run(() =>
            {
                if (!_serialPort.IsOpen) return string.Empty;

                lock (_gate)
                {
                    if (discardInBuffer)
                    {
                        try { _serialPort.DiscardInBuffer(); } catch { /* ignore */ }
                    }

                    _serialPort.ReadTimeout = timeoutMs ?? _defaultTimeoutMs;
                    _serialPort.WriteLine(mensaje);

                    try
                    {
                        var resp = _serialPort.ReadLine();
                        return resp ?? string.Empty;
                    }
                    catch (TimeoutException)
                    {
                        return string.Empty;
                    }
                }
            });
        }

        /// <summary>
        /// Envía y espera HASTA recibir exactamente 'expected'.
        /// - overallTimeoutMs: tiempo total máximo (null = indefinido).
        /// - ct: permite cancelar desde afuera sin colgar la UI.
        /// Lee por ventanas de 1s para poder checar cancelación/timeout.
        /// </summary>
        public Task<bool> WriteAndWaitForAsync(
            string mensaje,
            string expected,
            int? overallTimeoutMs = null,
            bool caseInsensitive = true,
            CancellationToken ct = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SerialWriterReader));

            return Task.Run(() =>
            {
                if (!_serialPort.IsOpen) return false;

                var sw = Stopwatch.StartNew();
                string Normalize(string s) => (s ?? string.Empty).Replace("\r", "").Replace("\n", "");

                lock (_gate)
                {
                    // limpiar entrada y enviar
                    try { _serialPort.DiscardInBuffer(); } catch { /* ignore */ }
                    _serialPort.WriteLine(mensaje);

                    while (true)
                    {
                        if (ct.IsCancellationRequested) return false;
                        if (overallTimeoutMs.HasValue && sw.ElapsedMilliseconds > overallTimeoutMs.Value) return false;

                        // leer en ventanas de 1s para checar cancelación/timeout
                        _serialPort.ReadTimeout = 1000;
                        try
                        {
                            string line = _serialPort.ReadLine();
                            line = Normalize(line);

                            if (caseInsensitive)
                            {
                                if (string.Equals(line, expected, StringComparison.OrdinalIgnoreCase))
                                    return true;
                            }
                            else
                            {
                                if (line == expected) return true;
                            }
                            // si llegó algo distinto, seguir esperando
                        }
                        catch (TimeoutException)
                        {
                            // no llegó línea en esta ventana: seguimos
                        }
                    }
                }
            }, ct);
        }
        // --- Sincrónico (si lo necesitas) ---
        public string WriteAndRead(string mensaje, int? timeoutMs = null, bool discardInBuffer = true)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SerialWriterReader));
            if (!_serialPort.IsOpen) return string.Empty;

            lock (_gate)
            {
                if (discardInBuffer)
                {
                    try { _serialPort.DiscardInBuffer(); } catch { /* ignore */ }
                }

                _serialPort.ReadTimeout = timeoutMs ?? _defaultTimeoutMs;
                _serialPort.WriteLine(mensaje);

                try
                {
                    var respuesta = _serialPort.ReadLine();
                    return respuesta ?? string.Empty;
                }
                catch (TimeoutException)
                {
                    return string.Empty;
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                if (_serialPort.IsOpen) _serialPort.Close();
            }
            catch { /* ignore */ }
            _serialPort.Dispose();
        }
    }
}