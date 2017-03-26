using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using System.Threading.Tasks;

namespace MAudio
{
    public sealed class MRenderStream : Stream
    {
        MQueueStream queue;
        Stream innerStream;
        long readByte = 0;
        bool bufferReadyFired = false;

        public event EventHandler BufferReady;
        public event EventHandler DataRequest;
        
        public MRenderStream(int bufferSize, int readyLength, int criticalLength)
        {
            queue = new MQueueStream(bufferSize);
            BufferReadyLength = readyLength;
            CriticalLength = criticalLength;
            InputStream = null;
        }

        int BufferReadyLength { get; set; }
        int CriticalLength { get; set; }

        #region Override member
        public override bool CanRead
        {
            get { return false; }
        }
        public override bool CanSeek
        {
            get { return false; }
        }
        public override bool CanWrite
        {
            get
            {
                return InputStream == null;
            }
        }
        public override void Flush()
        {
            queue.Flush();
            readByte = 0;
            bufferReadyFired = false;
        }
        public override long Length
        {
            get { return queue.Length; }
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
        public override int Read(byte[] buffer, int offset, int count)
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
        internal int Read(byte[] buffer)
        {
            if (Length - buffer.Length <= CriticalLength)
            {
                if (innerStream == null)
                {
                    if (DataRequest != null)
                        DataRequest(this, null);
                }
                else
                {
                    int minimumLength = CriticalLength - (int)Length + buffer.Length;

                    byte[] data = new byte[minimumLength];
                    int availableLength = innerStream.Read(data, 0, minimumLength);
                    queue.Write(data, 0, availableLength);
                }
            }

            int available = queue.Read(buffer, 0, buffer.Length);
            readByte += buffer.Length;

            return available;
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (innerStream != null)
                throw new NotSupportedException();

            queue.Write(buffer, offset, count);
            if (!bufferReadyFired && Length >= BufferReadyLength)
            {
                BufferReady(this, null);
                bufferReadyFired = true;
            }
        }
        public void Write(short[] buffer, int offset, int count)
        {
            byte[] bBuffer = new byte[count * 2];
            Buffer.BlockCopy(buffer, offset, bBuffer, 0, bBuffer.Length);

            Write(bBuffer, 0, bBuffer.Length);
        }
        public void Write(float[] buffer, int offset, int count)
        {
            byte[] bBuffer = new byte[count * 4];
            Buffer.BlockCopy(buffer, offset, bBuffer, 0, bBuffer.Length);

            Write(bBuffer, 0, bBuffer.Length);
        }
        #endregion

        public Stream InputStream
        {
            get { return innerStream; }
            set
            {
                if (value != null && !value.CanRead)
                    throw new ArgumentException();
                if (value != null)
                    bufferReadyFired = true;
                innerStream = value;
            }
        }
    }
}
