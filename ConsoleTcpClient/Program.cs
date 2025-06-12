using System;
using System.Text;
using TouchSocket.Core;
using TouchSocket.Sockets;


internal class Program
{
    static async Task Main(string[] args)
    {
        //var bytes = File.ReadAllBytes("D:\\1.jpg");
        var bytes = new byte[2000];
        Random.Shared.NextBytes(bytes);

        var client = await GetClient();
        //for (int j = 0; j < 10; j++)
        //{
        //    _ = Task.Run(async () =>
        //    {
        for (int i = 0; i < 1000; i++)
        {
            await client.SendAsync(BitConverter.GetBytes(bytes.LongLength));
            await client.SendAsync(Guid.NewGuid().ToByteArray());
            await client.SendAsync(BitConverter.GetBytes((ushort)PictureType.jpg));
            await client.SendAsync(bytes);

            //await client.CloseAsync();
            Console.WriteLine(i);
            await Task.Delay(1);
        }
        //    });
        //}

        Console.ReadLine();
    }

    static async Task<TcpClient> GetClient()
    {
        TcpClient client = new TcpClient();
        await client.SetupAsync(new TouchSocketConfig()
            .SetRemoteIPHost("10.0.0.30:17789")
            .ConfigureContainer(a =>
            {
                //a.AddLogger(new TouchLogAdapterILogger(logger));
                a.AddConsoleLogger();
            }).ConfigurePlugins(a =>
            {
                //a.UseCheckClear()
                //.SetCheckClearType(CheckClearType.All)
                //.SetTick(TimeSpan.FromSeconds(60))
                //.SetOnClose(async (c, t) =>
                //{
                //    //await c.ShutdownAsync();
                //    await c.CloseAsync("超时无数据");
                //});
            })); ;

        await client.ConnectAsync();

        return client;
    }
}

