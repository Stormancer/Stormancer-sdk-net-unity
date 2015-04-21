using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Dto
{
    public class RouteDto
    {
        public RouteDto()
        {

        }
        
        public string Name { get; set; }
        public ushort Handle { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }
}
