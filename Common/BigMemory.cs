
using Common;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

public class BigMemory
{
    public static MemoryInfo ReadFromFile(string path)
    {
        int bufferSize = 102400;
        MemoryInfo memoryInfo = new MemoryInfo();

        long offset = 0;
        FileInfo fi = new FileInfo(path);
        memoryInfo.Size = fi.Length;
        IntPtr point = Marshal.AllocHGlobal((nint)fi.Length);
        memoryInfo.Point = point;

        using var fs = fi.OpenRead();
        byte[] buffer = new byte[bufferSize];
        while (true)
        {
            //if (offset + bufferSize > memoryInfo.Size)
            //{
            //  bufferSize = (int)(memoryInfo.Size - offset);
            //}
            int readCount = fs.Read(buffer, 0, buffer.Length);
            if (readCount > 0)
            {
                Marshal.Copy(buffer, 0, point, readCount);
            }
            offset += readCount;
            if (offset >= memoryInfo.Size)
                break;
        }

        return memoryInfo;
    }

    public static void WriteToSteam(IntPtr point, long length, Stream stream)
    {
        int bufferSize = 4096;
        byte[] buffer = new byte[bufferSize];
        long offset = 0;

        while (true)
        {
            if (offset + bufferSize > length)
            {
                bufferSize = (int)(length - offset);
            }
            Marshal.Copy((nint)(point + offset), buffer, 0, bufferSize);
            stream.Write(buffer, 0, bufferSize);
            offset += bufferSize;
            if (offset >= length)
            {
                break;
            }
        }
    }

    public static void WriteToFile(IntPtr point, string path, long length)
    {
        int bufferSize = 4096;
        byte[] buffer = new byte[bufferSize];
        long offset = 0;
        using FileStream fs = File.OpenWrite(path);

        while (true)
        {
            if (offset + bufferSize > length)
            {
                bufferSize = (int)(length - offset);
            }
            Marshal.Copy((nint)(point + offset), buffer, 0, bufferSize);
            fs.Write(buffer, 0, bufferSize);
            offset += bufferSize;
            if (offset >= length)
            {
                break;
            }
        }
        fs.Flush();
        fs.Close();
    }

    public static void WriteToFile(Stream stream, string path, long length)
    {
        int bufferSize = 4096;
        byte[] buffer = new byte[bufferSize];
        long offset = 0;
        using FileStream fs = File.OpenWrite(path);

        while (true)
        {
            if (offset + bufferSize > length)
            {
                bufferSize = (int)(length - offset);
            }

            int readCount = stream.Read(buffer, 0, bufferSize);

            fs.Write(buffer, 0, bufferSize);
            offset += readCount;
            if (offset >= length)
            {
                break;
            }
        }
        fs.Flush();
        fs.Close();
    }

    public static void WriteToFile(ImageMemoryByteArray array, string path, long length)
    {
        int bufferSize = 4096;
        byte[] buffer = new byte[bufferSize];
        long offset = 0;
        using FileStream fs = File.OpenWrite(path);

        while (true)
        {
            if (offset + bufferSize > length)
            {
                bufferSize = (int)(length - offset);
            }

            var writespan = array.AsSpan(offset).Slice(0, bufferSize);

            fs.Write(writespan);
            offset += bufferSize;
            if (offset >= length)
            {
                break;
            }
        }
        fs.Flush();
        fs.Close();
    }

    public static void WriteToMulti(IntPtr point, long length, Action<byte[], int> readFromBuffer)
    {
        int bufferSize = 4096;
        byte[] buffer = new byte[bufferSize];
        long offset = 0;

        while (true)
        {
            if (offset + bufferSize > length)
            {
                bufferSize = (int)(length - offset);
            }
            Marshal.Copy((nint)(point + offset), buffer, 0, bufferSize);
            readFromBuffer(buffer, bufferSize);
            offset += bufferSize;
            if (offset >= length)
            {
                break;
            }
        }
    }


    public static void Copy(IntPtr p1, IntPtr p2, long length)
    {
        int bufferSize = int.MaxValue;
        long offset = 0;

        while (true)
        {
            if (offset + bufferSize > length)
            {
                bufferSize = (int)(length - offset);
            }
            //Copy
            offset += bufferSize;
            if (offset >= length)
            {
                break;
            }
        }
    }
}

public class MemoryInfo
{
    public IntPtr Point;
    public long Size;
}
