using Cysharp.Collections;
using TouchSocket.NamedPipe;
using TouchSocket.Sockets;


public static class TouchExtensions
{

    public static Task SendAsync(this IClientSender tcpClient, byte[] buffer, int startIndex, int length)
    {
        return tcpClient.SendAsync(buffer.AsMemory().Slice(0, length));
    }

    public static Task SendAsync(this IClientSender tcpClient, byte[] buffer)
    {
        return tcpClient.SendAsync(buffer.AsMemory());
    }

    public static async Task SendAsync(this IClientSender tcpClient, NativeMemoryArray<byte> buffer)
    {
        var list = buffer.AsReadOnlyMemoryList();
        foreach (var item in list)
        {
            await tcpClient.SendAsync(item);
        }
    }
}
