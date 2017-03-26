using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.IO;
using HNet.Converters;

namespace HNet
{
    public sealed class RateCalculatedEventArgs : EventArgs
    {
        public RateCalculatedEventArgs(double rate)
        {
            Rate = rate;
        }

        public double Rate {
            get; private set;
        }
    }

    public sealed class RatingStream : Stream
    {
        public event EventHandler<RateCalculatedEventArgs> ReadRateCalculated;
        public event EventHandler<RateCalculatedEventArgs> WriteRateCalculated;

        int readCount = 0;
        int writeCount = 0;

        Task reportTask;
        CancellationTokenSource tokenSource;

        public RatingStream(Stream innerStream, TimeSpan period)
        {
            InnerStream = innerStream;
            Period = period;

            tokenSource = new CancellationTokenSource();
            reportTask = ReportAsync();
        }
        public RatingStream(Stream innerStream) : this(innerStream, new TimeSpan(0, 0, 1)) { }

        async Task ReportAsync()
        {
            while (!tokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(Period);
                if (ReadRateCalculated != null)
                    ReadRateCalculated(this, new RateCalculatedEventArgs(readCount / Period.TotalSeconds));
                if (WriteRateCalculated != null)
                    WriteRateCalculated(this, new RateCalculatedEventArgs(writeCount / Period.TotalSeconds));
                lock (this)
                    readCount = writeCount = 0;
            }
        }
        public void Stop()
        {
            tokenSource.Cancel();
        }

        #region Override
        public override bool CanRead
        {
            get { return InnerStream.CanRead; }
        }
        public override bool CanSeek
        {
            get { return InnerStream.CanSeek; }
        }
        public override bool CanWrite
        {
            get { return InnerStream.CanWrite; }
        }
        public override void Flush()
        {
            InnerStream.Flush();
        }
        public override long Length
        {
            get { return InnerStream.Length; }
        }
        public override long Position
        {
            get
            {
                return InnerStream.Position;
            }
            set
            {
                InnerStream.Position = value;
            }
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            return InnerStream.Seek(offset, origin);
        }
        public override void SetLength(long value)
        {
            InnerStream.SetLength(value);
        }
        #endregion

        public override int Read(byte[] buffer, int offset, int count)
        {
            int byteRead = InnerStream.Read(buffer, offset, count);
            lock (this)
                readCount += byteRead;
            return byteRead;
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (this)
                writeCount += count;
            InnerStream.Write(buffer, offset, count);
        }

        #region Properties
        public Stream InnerStream {
            get; private set;
        }
        public TimeSpan Period
        {
            get;
            set;
        }
        #endregion
    }

    public class FloatRatingConverter : OutFloatConverter, FloatConverter
    {
        public event EventHandler<RateCalculatedEventArgs> RateCalculated;

        int writeCount = 0;

        Task reportTask;
        CancellationTokenSource tokenSource;

        public FloatRatingConverter(FloatConverter innerConverter, TimeSpan period) :
            base(innerConverter)
        {
            Period = period;

            tokenSource = new CancellationTokenSource();
            reportTask = ReportAsync();
        }
        public FloatRatingConverter(FloatConverter innerConverter) : this(innerConverter, new TimeSpan(0, 0, 1)) { }

        async Task ReportAsync()
        {
            while (!tokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(Period);
                if (RateCalculated != null)
                    RateCalculated(this, new RateCalculatedEventArgs(writeCount / Period.TotalSeconds));
                lock (this)
                    writeCount = 0;
            }
        }
        public void Stop()
        {
            tokenSource.Cancel();
        }

        public override void Write(float[] buffer)
        {
            lock (this)
                writeCount += buffer.Length * 4;
            base.Write(buffer);
        }
        public override void Write(byte[] buffer)
        {
            lock (this)
                writeCount += buffer.Length;
            base.Write(buffer);
        }

        #region Properties
        public TimeSpan Period
        {
            get;
            set;
        }
        #endregion
    }
    public class ShortRatingConverter : OutShortConverter, ShortConverter
    {
        public event EventHandler<RateCalculatedEventArgs> RateCalculated;

        int writeCount = 0;

        Task reportTask;
        CancellationTokenSource tokenSource;

        public ShortRatingConverter(ShortConverter innerConverter, TimeSpan period) :
            base(innerConverter)
        {
            Period = period;

            tokenSource = new CancellationTokenSource();
            reportTask = ReportAsync();
        }
        public ShortRatingConverter(ShortConverter innerConverter) : this(innerConverter, new TimeSpan(0, 0, 1)) { }

        async Task ReportAsync()
        {
            while (!tokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(Period);
                if (tokenSource.Token.IsCancellationRequested)
                    return;
                if (RateCalculated != null)
                    RateCalculated(this, new RateCalculatedEventArgs(writeCount / Period.TotalSeconds));
                lock (this)
                    writeCount = 0;
            }
        }
        public void Stop()
        {
            tokenSource.Cancel();
        }

        public override void Write(short[] buffer)
        {
            lock (this)
                writeCount += buffer.Length * 2;
            base.Write(buffer);
        }
        public override void Write(byte[] buffer)
        {
            lock (this)
                writeCount += buffer.Length;
            base.Write(buffer);
        }

        #region Properties
        public TimeSpan Period
        {
            get;
            set;
        }
        #endregion
    }
}
