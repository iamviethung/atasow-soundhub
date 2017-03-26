using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

using HNet.Converters;

namespace TEngine.Output
{
    public class WaveOutputConverter : FloatConverter
    {
        StorageFile file;
        IRandomAccessStream stream;
        IOutputStream outputstream;
        DataWriter datawriter;
        int size;

        public WaveOutputConverter() { }

        public async Task StartRecording()
        {
            StorageFolder folder = KnownFolders.MusicLibrary;
            string timestamp = DateTime.Now.ToString("yyyy.MM.dd-HH.mm.ss");
            file = await folder.CreateFileAsync("soundhub-" + timestamp + ".wav",
                CreationCollisionOption.ReplaceExisting);
            stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            outputstream = stream.GetOutputStreamAt(0);
            datawriter = new DataWriter(outputstream) { ByteOrder = ByteOrder.LittleEndian };
            size = 0;

            byte[] header = Encoding.UTF8.GetBytes("RIFF    WAVEfmt ");
            byte[] dataheader = Encoding.UTF8.GetBytes("data");

            datawriter.WriteBytes(header);
            datawriter.WriteUInt32(16);     // fmt chunk size = 16
            datawriter.WriteUInt16(3);      // Float audioformat = 3
            datawriter.WriteUInt16(1);      // No channels = 1
            datawriter.WriteUInt32(8000);   // Sample rate
            datawriter.WriteUInt32(32000);  // Byte rate
            datawriter.WriteUInt16(4);      // Block align
            datawriter.WriteUInt16(32);     // Bits per sample
            datawriter.WriteBytes(dataheader);
            datawriter.WriteUInt32(0);
            await datawriter.StoreAsync();
            await outputstream.FlushAsync();
        }
        public async Task StopRecording()
        {
            await datawriter.StoreAsync();
            await outputstream.FlushAsync();
            outputstream.Dispose();
            datawriter.Dispose();

            using (outputstream = stream.GetOutputStreamAt(4))
            using (datawriter = new DataWriter(outputstream) { ByteOrder = ByteOrder.LittleEndian })
            {
                datawriter.WriteUInt32((uint)size + 44);
                await datawriter.StoreAsync();
                await outputstream.FlushAsync();
            }

            using (outputstream = stream.GetOutputStreamAt(40))
            using (datawriter = new DataWriter(outputstream) { ByteOrder = ByteOrder.LittleEndian })
            {
                datawriter.WriteUInt32((uint)size);
                await datawriter.StoreAsync();
                await outputstream.FlushAsync();
            }
        }

        public void Write(float[] buffer)
        {
            byte[] bBuffer = new byte[buffer.Length * 4];
            System.Buffer.BlockCopy(buffer, 0, bBuffer, 0, bBuffer.Length);
            Write(bBuffer);
        }
        public void Write(byte[] buffer)
        {
            datawriter.WriteBytes(buffer);
            size += buffer.Length;
        }

        public double Time { get { return size / 32000.0; } }
        public string FileName { get { return file.Name; } }
    }
}
