using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfGMTraceability.Models
{
    public class StationData
    {
        public string Station { get; set; }
        public List<Part> Parts { get; set; }
    }
    public class Part
    {
        public string BomPart { get; set; }
        public int bom_quantity_per_piece { get; set; }
        public int total_available { get; set; }
        public bool Sufficient { get; set; }
        public List<Box> Boxes { get; set; }
    }
    public class Box
    {
        public string BoxNumber { get; set; }
        public int BoxQt { get; set; }
    }
}
