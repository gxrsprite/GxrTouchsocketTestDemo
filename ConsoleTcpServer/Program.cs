using System.Threading.Tasks;
using TouchSocket.Core;
using TouchSocket.Sockets;

namespace ConsoleTcpServer
{
    internal class Program
    {
        static long count = 0;
        static async Task Main(string[] args)
        {
            var server = GetTcpService();
            //server.Connecting = async (client, e) => //有客户端正在连接
            //{
            //    e.Id = "pc";
            //};

            //await Task.Delay(15000);
            Console.ReadLine();

            //server.Send("pc", "aabb");
            server.Dispose();
            Console.ReadKey();
        }

        static async Task<TcpService> GetTcpService()
        {
            TcpService service = new TcpService();
            service.Received = async (client, e) =>
            {
                //接收信息，在CustomDataHandlingAdapter派生的适配器中，byteBlock将为null，requestInfo将为适配器定义的泛型
                if (e.RequestInfo is BigFixedHeaderRequestInfo myRequestInfo)
                {
                    var crtcount = Interlocked.Increment(ref count);
                    Console.WriteLine($"Guid:{myRequestInfo.Guid}");
                    Console.WriteLine($"AllCount:{crtcount}");
                }
            };

            await service.SetupAsync(new TouchSocketConfig()//载入配置     
                .SetListenIPHosts(new IPHost[] { new IPHost(17789) })
                .ConfigureContainer(a =>
                {
                    //a.AddLogger(new TouchLogAdapterILogger(logger));
                    a.AddConsoleLogger();
                })
                .SetTcpDataHandlingAdapter(() => { return new BigFixedHeaderCustomDataHandlingAdapter(); })
                .ConfigurePlugins(a =>
                {
                }));//配置适配器
            await service.StartAsync();//启动

            return service;
        }
    }
}
