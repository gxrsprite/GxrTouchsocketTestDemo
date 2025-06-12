using Cysharp.Collections;
using System.Buffers;
using System.ComponentModel.DataAnnotations;
using System.Reflection.PortableExecutable;
using TouchSocket.Core;

public class BigUnfixedHeaderRequestInfo : IBigUnfixedHeaderRequestInfo
{
    const int hairLength = 16;
    public long BodyLength { get; private set; }
    public int DataType { get; private set; }

    public NativeMemoryArray<byte> Bytes { get; private set; }

    /// <summary>
    /// 头部总长度
    /// </summary>
    public int HeaderLength { get; set; }
    public byte[] HeaderData { get; private set; }

    IBufferWriter<byte> writer;

    public bool OnFinished()
    {
        return true;
    }

    public bool OnParsingHeader<TByteBlock>(ref TByteBlock byteBlock) where TByteBlock : IByteBlock
    {
        if (byteBlock.CanReadLength < 4)//判断是否能获取头部长度
        {
            return false;
        }

        if (this.HeaderLength == 0)
        {//获取头部长度
            this.HeaderLength = byteBlock.ReadInt32();
        }
        else if (this.HeaderLength > byteBlock.CanReadLength) //长度足够后才解析
        {
            return false;
        }
        //头部读取完毕，开始解析
        DataType = byteBlock.ReadInt32();
        BodyLength = byteBlock.ReadInt64();
        if (HeaderLength > hairLength)
        {
            HeaderData = byteBlock.ReadToSpan(HeaderLength - hairLength).ToArray();
        }

        Bytes = new NativeMemoryArray<byte>(BodyLength);
        writer = Bytes.CreateBufferWriter();
        return true;
    }

    public void OnAppendBody(ReadOnlySpan<byte> buffer)
    {
        writer.Write(buffer);
    }
}

public class BigUnfixedHeaderCustomDataHandlingAdapter : CustomBigUnfixedHeaderDataHandlingAdapter<BigUnfixedHeaderRequestInfo>
{
    protected override BigUnfixedHeaderRequestInfo GetInstance()
    {
        return new BigUnfixedHeaderRequestInfo();
    }
}
