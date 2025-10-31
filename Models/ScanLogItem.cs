using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfGMTraceability.Models
{
    public class ScanLogItem
    {        
        public string Title { get; set; }
        public string Serial { get; set; }
        public string APIResponse { get; set; }
        public string APIStatus { get; set; }
        public string Msj { get; set; }
        public string MsjType { get; set; } // "Error", "Advertencia", "Info"
        public DateTime Timestamp { get; set; }
        public bool Persistent { get; set; } = false;
        public string Formatted
        {
            get
            {
                var exMsj = !string.IsNullOrWhiteSpace(Msj) ? Environment.NewLine + Msj : "";
                return $"{Title}{Environment.NewLine}[Hora]: {Timestamp:HH:mm:ss}      [API Response]: {APIResponse}      [API Status]: {APIStatus}{Environment.NewLine}[Serial]: {Serial}{exMsj}";
            }
        }            
    }
}
