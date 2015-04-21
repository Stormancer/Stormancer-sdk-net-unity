using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stormancer
{
    public class ClientException : Exception
    {
        public ClientException(string msg) : base(msg) { }
    }
}
