using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace HNet.Converters
{
    public class StreamConverter : Converter
    {
        public StreamConverter(Stream innerStream)
        {
            InnerStream = innerStream;

            if (!innerStream.CanWrite)
                throw new ArgumentException("InnerStream must be able to be written");
        }

        public void Write(byte[] buffer)
        {
            InnerStream.Write(buffer, 0, buffer.Length);
        }
        public void Flush()
        {
            InnerStream.Flush();
        }

        public Stream InnerStream { get; private set; }
    }

    public class ShortStreamConverter : StreamConverter, ShortConverter
    {
        public ShortStreamConverter(Stream innerStream) :
            base(innerStream) { }

        public void Write(short[] buffer)
        {
            byte[] bBuffer = new byte[buffer.Length * 2];
            Buffer.BlockCopy(buffer, 0, bBuffer, 0, bBuffer.Length);
            Write(bBuffer);
        }

    }

    public class FloatStreamConverter : StreamConverter, FloatConverter
    {
        public FloatStreamConverter(Stream innerStream) :
            base(innerStream) { }

        public void Write(float[] buffer)
        {
            byte[] bBuffer = new byte[buffer.Length * 4];
            Buffer.BlockCopy(buffer, 0, bBuffer, 0, bBuffer.Length);
            Write(bBuffer);
        }

    }

    public class ConverterStream : Stream
    {
        public ConverterStream(Converter innerConverter)
        {
            InnerConverter = innerConverter;
        }

        #region Override
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
            get { return true; }
        }
        public override void Flush()
        {
        }
        public override long Length
        {
            get { throw new NotSupportedException(); }
        }
        public override long Position
        {
            get
            {
                throw new NotSupportedException();
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

        public override void Write(byte[] buffer, int offset, int count)
        {
            byte[] bBuffer = new byte[count];
            Buffer.BlockCopy(buffer, offset, bBuffer, 0, count);
            InnerConverter.Write(bBuffer);
        }

        public Converter InnerConverter {
            get; private set;
        }
    }
}
