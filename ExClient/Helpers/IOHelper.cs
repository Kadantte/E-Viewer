﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Foundation;
using static System.Runtime.InteropServices.WindowsRuntime.AsyncInfo;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Windows.Storage
{
    public static class IOHelper
    {
        public static IRandomAccessStream AsRandomAccessStream(this IBuffer buffer)
        {
            return buffer.AsStream().AsRandomAccessStream();
        }

        public static IRandomAccessStream AsRandomAccessStream(this byte[] data)
        {
            return data.AsBuffer().AsRandomAccessStream();
        }
    }
}
