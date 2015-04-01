using RakNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{
    static class Buffers
    {
        internal static Stream ToStream(this ArraySegment<byte> buffer)
        {
            return new MemoryStream(buffer.Array, buffer.Offset, buffer.Count);
        }
        internal static ArraySegment<T> Offset<T>(this ArraySegment<T> buffer, int offset)
        {
            return new ArraySegment<T>(buffer.Array, buffer.Offset + offset, buffer.Count - offset);
        }

        internal static void Write(this BitStream bs, ArraySegment<byte> data)
        {
            byte[] buffer;
           
            if(data.Offset == 0)
            {
                buffer = data.Array; 
            }
            else
            {
                buffer = new byte[data.Count];
                Buffer.BlockCopy(data.Array, data.Offset, buffer, 0, data.Count);
                
            }
            bs.Write(buffer,(uint)data.Count);
        }
    }
}
