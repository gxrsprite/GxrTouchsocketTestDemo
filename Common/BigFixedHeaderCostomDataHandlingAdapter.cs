
using Cysharp.Collections;
using System.Buffers;
using TouchSocket.Core;

public enum PictureType : ushort
{
    Unknow = 0,
    tiff,
    png,
    jpg,
    webp,
    binary
}

class PictureTypeConverter
{
    public static PictureType FromExtension(string ext)
    {
        switch (ext)
        {
            case ".tiff":
                return PictureType.tiff;
            case ".tif":
                return PictureType.tiff;
            case ".png":
                return PictureType.png;
            case ".jpg":
                return PictureType.jpg;
            case ".webp":
                return PictureType.webp;
            default:
                return PictureType.tiff;

        }

    }

}

public class BigFixedHeaderRequestInfo : IBigFixedHeaderRequestInfo
{
    static long count;
    static long count2;

    public long BodyLength { get; private set; }

    public Guid Guid { get; private set; }

    public NativeMemoryArray<byte> Bytes { get; private set; }

    public PictureType PictureType { get; private set; }

    IBufferWriter<byte> writer;

    bool isFinished = false;
    long currentNunber = 0;

    public bool OnFinished()
    {
        isFinished = true;
        var crtcount2 = Interlocked.Increment(ref count2);
        Console.WriteLine("接受完成数据：" + crtcount2);
        return true;
    }

    public bool OnParsingHeader(ReadOnlySpan<byte> header)
    {
        currentNunber = Interlocked.Increment(ref count);
        Console.WriteLine("接受完成数据：" + currentNunber);
        BodyLength = BitConverter.ToInt64(header.Slice(0, 8).ToArray());
        Guid = new Guid(header[8..24]);
        PictureType = (PictureType)BitConverter.ToUInt16(header.Slice(24, 2));
        Bytes = new NativeMemoryArray<byte>(BodyLength);
        writer = Bytes.CreateBufferWriter();
        Task.Run(async () =>
        {
            await Task.Delay(5000);
            if (!isFinished)
            {
                Console.WriteLine("传输失败" + currentNunber);
            }

        });
        return true;
    }

    public void OnAppendBody(ReadOnlySpan<byte> buffer)
    {
        writer.Write(buffer);
    }
}

public class BigFixedHeaderCustomDataHandlingAdapter : CustomBigFixedHeaderDataHandlingAdapter<BigFixedHeaderRequestInfo>
{
    public override int HeaderLength => 26;

    protected override BigFixedHeaderRequestInfo GetInstance()
    {
        return new BigFixedHeaderRequestInfo();
    }
}