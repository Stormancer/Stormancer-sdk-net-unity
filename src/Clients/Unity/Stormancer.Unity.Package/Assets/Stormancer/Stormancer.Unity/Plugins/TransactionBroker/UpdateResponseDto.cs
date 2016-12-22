using MsgPack.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stormancer.Plugins.TransactionBroker
{
    public class UpdateResponseDto
    {
        public UpdateResponseDto()
        {
            Reason = "";
        }

        [MessagePackMember(0)]
        public bool Success { get; set; }

        [MessagePackMember(1)]
        public string Reason;

        [MessagePackMember(2)]
        public int Hash { get; set; }
    }
}
