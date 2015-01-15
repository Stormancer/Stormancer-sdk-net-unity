using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stormancer.Core
{
    public enum PacketReliability
    {
        UNRELIABLE = 0,
        UNRELIABLE_SEQUENCED = 1,
        RELIABLE = 2,
        RELIABLE_ORDERED = 3,
        RELIABLE_SEQUENCED = 4,
        UNRELIABLE_WITH_ACK_RECEIPT = 5,
        RELIABLE_WITH_ACK_RECEIPT = 6,
        RELIABLE_ORDERED_WITH_ACK_RECEIPT = 7,
        NUMBER_OF_RELIABILITIES = 8,
    }
}
