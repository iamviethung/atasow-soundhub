using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using System.Threading.Tasks;

namespace MAudio
{
    public sealed class MCaptureStream : Stream
    {
        MQueueStream queue;
        Stream innerStream;
        long readByte = 0;

        public event EventHandler DataReady;

        public MCaptureStream(int bufferSize)
        {
            queue = new MQueueStream(bufferSize);
            innerStream = null;
        }

        #region Override member
        public override bool CanRead
        {
            get { return OutputStream == null; }
        }
        public override bool CanSeek
        {
            get { return false; }
        }
        public override bool CanWrite
        {
            get { return false; }
        }
        public override void Flush()
        {
            readByte = 0;
            if (innerStream == null)
                queue.Flush();
            else
                innerStream.Flush();
        }
        public override long Length
        {
            get
            {
                if (innerStream == null) return queue.Length;
                else return innerStream.Length;
            }
        }
        public override long Position
        {
            get
            {
                return readByte;
            }
            set
            {
                throw new NotSupportedException();
            }
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
        #endregion

        #region Read/Write functions
        internal void Write(byte[] buffer)
        {
            if (innerStream == null)
            {
                queue.Write(buffer, 0, buffer.Length);
                DataReady(this, null);
            }
            else
            {
                innerStream.Write(buffer, 0, buffer.Length);
                readByte += buffer.Length;
            }
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            return queue.Read(buffer, offset, count);
        }
        public int Read(short[] buffer, int offset, int count)
        {
            byte[] bBuffer = new byte[count * 2];
            int available = Read(bBuffer, 0, bBuffer.Length);

            Buffer.BlockCopy(bBuffer, 0, buffer, offset, bBuffer.Length);
            return available / 2;
        }
        public int Read(float[] buffer, int offset, int count)
        {
            byte[] bBuffer = new byte[count * 4];
            int available = Read(bBuffer, 0, bBuffer.Length);

            Buffer.BlockCopy(bBuffer, 0, buffer, offset, bBuffer.Length);
            return available / 4;
        }
        #endregion

        public Stream OutputStream
        {
            get { return innerStream; }
            set
            {
                if (value != null && !value.CanWrite)
                    throw new ArgumentException();
                if (value != null && innerStream == null) {
                    byte[] buffer = new byte[queue.Length];
                    queue.Read(buffer, 0, buffer.Length);
                    value.Write(buffer, 0, buffer.Length);
                    readByte += buffer.Length;
                    queue.Flush();
                }
                innerStream = value;
            }
        }
    }
}
