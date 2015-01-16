using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Dto
{
    public class Empty
    {
        private static Empty _instance = new Empty();
        public static Empty Instance
        {
            get
            {
                return _instance;
            }
        }
    }
}
