using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfGMTraceability.Helpers
{
    public interface IOverlayAware
    {
        event EventHandler ShowLoadOverlay;
        event EventHandler HideLoadOverlay;
    }
}
