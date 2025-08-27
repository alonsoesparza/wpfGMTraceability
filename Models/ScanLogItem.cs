using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfGMTraceability.Models
{
    public class ScanLogItem
    {        
        public string Msj { get; set; }
        public string MsjType { get; set; } // "Error", "Advertencia", "Info"
        public DateTime Timestamp { get; set; }
        public bool Persistent { get; set; } = false;
        public string Formatted => $"[{Timestamp:HH:mm:ss}] {Msj}";
    }
}
