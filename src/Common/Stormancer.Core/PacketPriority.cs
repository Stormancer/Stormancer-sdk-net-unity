using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stormancer.Core
{    
    public enum PacketPriority
    {
        IMMEDIATE_PRIORITY = 0,
        HIGH_PRIORITY = 1,
        MEDIUM_PRIORITY = 2,
        LOW_PRIORITY = 3,
        NUMBER_OF_PRIORITIES = 4,
    }
}
