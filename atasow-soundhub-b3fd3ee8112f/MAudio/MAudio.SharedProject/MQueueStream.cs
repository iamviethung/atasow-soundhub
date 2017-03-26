using System;
using System.Collections.Generic;
using System.Text;

using System.IO;

namespace MAudio
{
    public class MQueueStream : Stream
    {
        LinkedList<byte[]> data;
        int readOffset, writeOffset;
        long readByte;
        int bufferSize;

        object lockHandle;

        public MQueueStream(int bufferSize)
        {
            this.bufferSize = bufferSize;

            lockHandle = new object();

            data = new LinkedList<byte[]>();
            Flush();
        }

        #region Override member
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
            lock (lockHandle)
            {
                data.Clear();
                data.AddLast(new byte[bufferSize]);
                readByte = readOffset = writeOffset = 0;
            }
        }
        public override long Length
        {
            get { return (data.Count - 1) * bufferSize + writeOffset - readOffset; }
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
        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (lockHandle)
            {
                int currentCount = count;
                int currentOffset = offset;

                while (currentCount > 0 && data.Count > 1)
                {
                    int readSize = Math.Min(currentCount, bufferSize - readOffset);
                    Buffer.BlockCopy(data.First.Value, readOffset, buffer, currentOffset, readSize);

                    currentOffset += readSize;
                    currentCount -= readSize;
                    readOffset += readSize;
                    if (readOffset >= bufferSize)
                    {
                        readOffset -= bufferSize;
                        data.RemoveFirst();
                    }
                }

                if (currentCount > 0)
                {
                    int readSize = Math.Min(currentCount, writeOffset - readOffset);
                    Buffer.BlockCopy(data.First.Value, readOffset, buffer, currentOffset, readSize);

                    currentOffset += readSize;
                    currentCount -= readSize;
                    readOffset += readSize;
                }

                readByte += count - currentCount;

                return count - currentCount;
            }
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (lockHandle)
            {
                int currentCount = count;
                int currentOffset = offset;

                while (currentCount > 0)
                {
                    int writeSize = Math.Min(currentCount, bufferSize - writeOffset);
                    Buffer.BlockCopy(buffer, currentOffset, data.Last.Value, writeOffset, writeSize);

                    currentOffset += writeSize;
                    currentCount -= writeSize;
                    writeOffset += writeSize;
                    if (writeOffset >= bufferSize)
                    {
                        writeOffset -= bufferSize;
                        data.AddLast(new byte[bufferSize]);
                    }
                }
            }
        }
        #endregion
    }
}
