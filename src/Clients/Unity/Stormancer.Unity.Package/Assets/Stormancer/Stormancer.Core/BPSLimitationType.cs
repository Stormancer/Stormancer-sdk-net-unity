using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stormancer.Core
{    
    public enum BPSLimitationType : byte
    {
        None = 0,
        CongestionControl = 1,
        OutgoingBandwidth = 2
    }
}
