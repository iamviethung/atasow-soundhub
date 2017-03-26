using System;
using System.Collections.Generic;
using System.IO;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Core;

namespace HNet.Converters
{
    public class Logger : Converter
    {
        Action<string> func;

        public Logger(Action<string> logFunc)
        {
            func = logFunc;
        }
        
        public void Write(byte[] buffer)
        {
            string msg = System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            Write(msg);
        }
        public void Write(string message)
        {
            func(message);
        }
    }
    class FloatLogger : Logger, FloatConverter 
    {
        public FloatLogger(Action<string> logFunc)
            : base(logFunc)
        { }

        public void Write(float[] buffer)
        {
            string output = "";
            for (int i = 0; i < buffer.Length; i ++)
                output += buffer[i].ToString("F3") + " ";

            Write(output);
        }
    }
    class ShortLogger : Logger, ShortConverter
    {
        public ShortLogger(Action<string> logFunc)
            : base(logFunc)
        { }

        public void Write(short[] buffer)
        {
            string output = "";
            for (int i = 0; i < buffer.Length; i++)
                output += buffer[i].ToString() + " ";

            Write(output);
        }
    }
}