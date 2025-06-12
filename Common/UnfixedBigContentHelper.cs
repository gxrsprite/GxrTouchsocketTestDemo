using MemoryPack;
using TouchSocket.Core;
public class TcpUnfixedHeaderHelper
{
    public static byte[] GenerateHeader(int type, long bodylength, byte[] headerContent)
    {
        int headerLength = headerContent.Length + 16;
        byte[] allHeader = new byte[headerLength];

        Array.Copy(BitConverter.GetBytes(headerLength), allHeader, 4);
        Array.Copy(BitConverter.GetBytes(type), 0, allHeader, 4, 4);
        Array.Copy(BitConverter.GetBytes(bodylength), 0, allHeader, 8, 8);
        Array.Copy(headerContent, 0, allHeader, 16, headerContent.Length);

        return allHeader;
    }

    //public static ByteBlock GenerateHeaderSerializByTouch<T>(int type, long bodylength, T data) where T : PackageBase
    //{
    //    ByteBlock byteBlock = new ByteBlock();
    //    int headerLength = 16;

    //    byteBlock.WriteInt32(headerLength);
    //    byteBlock.WriteInt32(type);
    //    byteBlock.WriteInt64(bodylength);
    //    byteBlock.WritePackage(data);

    //    byteBlock.Position = 0;
    //    byteBlock.WriteInt32(byteBlock.Length + 16);

    //    return byteBlock;
    //}

    public static byte[] GenerateHeaderSerializByMemory<T>(int type, long bodylength, T data) where T : IMemoryPackable<T>
    {
        var headerContent = MemoryPack.MemoryPackSerializer.Serialize(data);
        int headerLength = headerContent.Length + 16;
        byte[] allHeader = new byte[headerLength];

        Array.Copy(BitConverter.GetBytes(headerLength), allHeader, 4);
        Array.Copy(BitConverter.GetBytes(type), 0, allHeader, 4, 4);
        Array.Copy(BitConverter.GetBytes(bodylength), 0, allHeader, 8, 8);
        Array.Copy(headerContent, 0, allHeader, 16, headerContent.Length);

        return allHeader;
    }

}
