using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stormancer.Unity.Models
{
    public class PositionUpdate
    {
        public DtoVector Position { get; set; }
        public DtoQuaternion Rotation { get; set; }
        public string Id { get; set; }
    }
}
