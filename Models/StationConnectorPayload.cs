using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MaterialDesignThemes.Wpf.Theme.ToolBar;

namespace wpfGMTraceability.Models
{
    public class StationConnectorPayload
    {
        public string station_name { get; set; }
        public List<object> items { get; set; }
    }
}
