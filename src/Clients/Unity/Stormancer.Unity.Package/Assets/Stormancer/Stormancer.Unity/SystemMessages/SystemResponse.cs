using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Core.Infrastructure.Messages
{
    public class SystemResponse
    {
        public bool IsError { get; set; }
        public string Message { get; set; }
    }
}
