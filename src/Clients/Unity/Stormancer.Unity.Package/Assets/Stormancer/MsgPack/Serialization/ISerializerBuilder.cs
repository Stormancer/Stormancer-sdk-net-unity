#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MsgPack.Serialization
{
    internal interface ISerializerBuilder
    {
        IMessagePackSingleObjectSerializer CreateArraySerializer();
        IMessagePackSingleObjectSerializer CreateMapSerializer();
        IMessagePackSingleObjectSerializer CreateSerializer();
    }
}
#endif