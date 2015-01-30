using RakNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Infrastructure
{
    internal class BSStream : Stream
    {
        private readonly BitStream bs;
        public BSStream(BitStream bs)
        {
            this.bs = bs;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {

        }

        public override long Length
        {
            get { return Position; }
        }
        private long _position;
        public override long Position
        {
            get
            {
                return _position;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count <= 0)
            {
                throw new ArgumentException("Count must be a positive value.");
            }
            if (offset == 0)
            {
                if (bs.ReadAlignedBytes(buffer, (uint)count))
                {
                    _position += count;
                    return count;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                var temp = new byte[count];
                if (bs.ReadAlignedBytes(temp, (uint)count))
                {
                    _position += count;
                    temp.CopyTo(buffer, offset);
                    return count;
                }
                else
                {
                    return 0;
                }
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count < 0)
            {
                throw new ArgumentException("Count must be a positive value.");
            }
            if (offset == 0)
            {
                bs.WriteAlignedBytes(buffer, (uint)count);
            }
            else
            {
                var temp = new byte[count];
                Buffer.BlockCopy(buffer, offset, temp, 0, count);
                
                bs.WriteAlignedBytes(temp, (uint)count);
            }
            _position += count;
        }
    }
}
