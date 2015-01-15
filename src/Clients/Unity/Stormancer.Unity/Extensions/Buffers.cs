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

            if (data.Offset == 0)
            {
                buffer = data.Array;
            }
            else
            {
                buffer = new byte[data.Count];
                Buffer.BlockCopy(data.Array, data.Offset, buffer, 0, data.Count);

            }
            bs.Write(buffer, (uint)data.Count);
        }

        //We pick a value that is the largest multiple of 4096 that is still smaller than the large object heap threshold (85K).
        // The CopyTo/CopyToAsync buffer is short-lived and is likely to be collected at Gen0, and it offers a significant
        // improvement in Copy performance.
        private const int _DefaultCopyBufferSize = 81920;

        public static void CopyTo(this Stream stream, Stream destination)
        {
            var buffer = new byte[_DefaultCopyBufferSize];
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                destination.Write(buffer, 0, read);
            }
        }
    }
}
