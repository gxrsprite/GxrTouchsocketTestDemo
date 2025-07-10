// 使用 CustomFixedHeaderDataHandlingAdapter + IFixedHeaderRequestInfo 实现可靠协议
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TouchSocket.Core;
using TouchSocket.Sockets;

namespace ReliableProtocolExample
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("1. Server\n2. Client");
            if (Console.ReadLine() == "1")
            {
                await RunServer();
            }
            else
            {
                await RunClient();
            }
        }

        static async Task RunServer()
        {
            var server = new TcpService();
            await server.SetupAsync(new TouchSocketConfig()
                .SetListenIPHosts("127.0.0.1:7789")
                .SetAdapterOption(new AdapterOption()
                {
                    MaxPackageSize = int.MaxValue
                })
                .SetTcpDataHandlingAdapter(() =>
                {
                    return new MyCustomAdapter();
                })
            //.ConfigurePlugins(p =>
            //{
            //    p.Add<MyServerPlugin>();
            //})
            );
            server.Received = async (c, e) =>
            {
                if (e.RequestInfo is DataRequestInfo dataRequestInfo)
                {
                    if (dataRequestInfo.Flags == 0x00)
                    {  //Console.WriteLine($"Received Seq={packet.Seq}, Size={packet.Payload.Length}");
                        var ack = DataRequestInfo.Build(dataRequestInfo.Seq, 0x01, Array.Empty<byte>(), 0, 0, dataRequestInfo.FileGuid, 0);//ack
                        await server.SendAsync(c.Id, ack.BuildHeader());
                    }

                }
            };
            await server.StartAsync();
            Console.WriteLine("Server started.");
            Console.ReadLine();
        }

        static async Task RunClient()
        {
            var client = new TouchSocket.Sockets.TcpClient();
            var ackWaiters = new ConcurrentDictionary<uint, TaskCompletionSource<bool>>();

            client.Received = async (c, e) =>
            {
                if (e.RequestInfo is DataRequestInfo ack)
                {
                    if (ack.Flags == 0x01 || ack.Flags == 0x02)
                    {
                        if (ackWaiters.TryRemove(ack.Seq, out var tcs))
                        {
                            tcs.TrySetResult(ack.Flags == 0x01);
                        }
                    }
                }
            };

            await client.SetupAsync(new TouchSocketConfig()
                .SetRemoteIPHost("127.0.0.1:7789")
                .SetTcpDataHandlingAdapter(() =>
                {
                    return new MyCustomAdapter();
                })
            );

            await client.ConnectAsync();

            // 模拟大数据发送
            byte[] bigData = new byte[101 * 1024 * 1024];
            new Random().NextBytes(bigData);
            var guid = Guid.NewGuid();
            int chunkSize = 20 * 1024 * 1024;//
            uint seq = 0;
            for (long offset = 0; offset < bigData.Length; offset += chunkSize, seq++)
            {
                long len = Math.Min(chunkSize, bigData.Length - offset);
                byte[] payload = new byte[len];
                Array.Copy(bigData, offset, payload, 0, len);
                double count = (double)bigData.Length / chunkSize;
                uint totalcount = (uint)Math.Round(count, MidpointRounding.ToPositiveInfinity);
                var packet = DataRequestInfo.Build(seq, 0x00, payload, (ulong)bigData.Length, totalcount, guid, 1);
                bool success = false;
                for (int retry = 0; retry < 3 && !success; retry++)
                {
                    Console.WriteLine($"Sending Seq={seq}, Attempt={retry + 1}");

                    await client.SendAsync(packet.BuildHeader());
                    await client.SendAsync(payload);
                    await Task.Delay(1);
                    var tcs = new TaskCompletionSource<bool>();
                    ackWaiters[seq] = tcs;

                    if (await Task.WhenAny(tcs.Task, Task.Delay(5000)) == tcs.Task)
                    {
                        success = tcs.Task.Result;
                    }
                    if (!success)
                    {
                        await Task.Delay(1000);
                    }
                }
                if (!success)
                {
                    Console.WriteLine($"Failed Seq={seq}");
                    break;
                }
            }
            Console.WriteLine("All sent");
            Console.ReadLine();
        }
    }

    public class DataRequestInfo : IFixedHeaderRequestInfo, IRequestInfoBuilder
    {

        public byte Flags { get; set; }
        public uint Seq { get; set; }
        public byte[] Payload { get; set; }
        /// <summary>
        /// 总分片数量
        /// </summary>
        public uint TotalCount { get; set; } = 1;
        /// <summary>
        /// 总字节数
        /// </summary>
        public ulong TotalLength { get; set; }

        public int BodyLength { get; set; }
        public Guid FileGuid { get; set; }
        public int FileType { get; set; }
        public int Reserved1 { get; set; }
        public int Reserved2 { get; set; }

        public int MaxLength => int.MaxValue; // 最大 2GB
        public byte[] Md5 { get; private set; }
        public bool IsMd5Right { get; private set; }

        public static DataRequestInfo Build(uint seq, byte flags, byte[] payload = null)
        {
            ArgumentNullException.ThrowIfNull(payload, nameof(payload));
            return new DataRequestInfo
            {
                Flags = flags,
                Seq = seq,
                Payload = payload,
                TotalLength = (ulong)(payload == null ? 0 : payload.Length),
            };
        }


        public static DataRequestInfo Build(uint seq, byte flags, byte[] payload, ulong totalLen, uint totalCount, Guid fileGuid, int fileType)
        {
            return new DataRequestInfo
            {
                Flags = flags,
                Seq = seq,
                Payload = payload,
                TotalLength = totalLen,
                TotalCount = totalCount,
                FileGuid = fileGuid,
                FileType = fileType
            };
        }

        public void Build<TByteBlock>(ref TByteBlock byteBlock) where TByteBlock : IByteBlock
        {
            var buffer = BuildHeader();
            byteBlock.Write(buffer);

            byteBlock.Write(Payload);

        }


        public byte[] BuildHeader()
        {
            byte[] buffer = new byte[69]; // 固定头部长度
            Span<byte> span = buffer.AsSpan();

            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(0, 2), 0x1001);
            span[2] = Flags;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(3, 4), Seq);
            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(7, 4), Payload.Length);

            using var md5 = System.Security.Cryptography.MD5.Create();
            byte[] hash = md5.ComputeHash(Payload);
            hash.CopyTo(span.Slice(11, 16));

            BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(27, 8), TotalLength);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(35, 4), TotalCount);
            FileGuid.ToByteArray().CopyTo(span.Slice(39, 16));
            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(55, 4), FileType);
            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(59, 4), Reserved1);
            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(63, 4), Reserved2);

            // CRC16
            var crc16bytes = Crc.Crc16(span.Slice(0, 67));
            crc16bytes.CopyTo(span.Slice(67, 2));

            return buffer;
        }

        /// <summary>
        /// 从 69字节包头中解析出字段，并返回 body 长度
        /// </summary>
        public bool OnParsingHeader(ReadOnlySpan<byte> header)
        {
            if (header.Length < 69)
                return false;

            ushort magic = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(0, 2));
            if (magic != 0x1001)
                return false;

            Flags = header[2];

            Seq = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(3, 4));
            var l1 = BinaryPrimitives.ReadInt32LittleEndian(header.Slice(7, 4));
            var l2 = BinaryPrimitives.ReadInt32BigEndian(header.Slice(7, 4));
            BodyLength = BinaryPrimitives.ReadInt32LittleEndian(header.Slice(7, 4));
            Md5 = header.Slice(11, 16).ToArray();
            TotalLength = BinaryPrimitives.ReadUInt64LittleEndian(header.Slice(27, 8));
            TotalCount = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(35, 4));
            FileGuid = new Guid(header.Slice(39, 16));
            FileType = BinaryPrimitives.ReadInt32LittleEndian(header.Slice(55, 4));
            Reserved1 = BinaryPrimitives.ReadInt32LittleEndian(header.Slice(59, 4));
            Reserved2 = BinaryPrimitives.ReadInt32LittleEndian(header.Slice(63, 4));
            if (BodyLength < 0 || BodyLength > 1024 * 1024 * 1024)
            {
                return false;
            }
            var crc16bytes = Crc.Crc16(header.Slice(0, 67));
            var crcChecked = crc16bytes.AsSpan().SequenceEqual(header.Slice(67, 2));
            if (BodyLength != 0)
            {
                Console.WriteLine($"Recive Header: Seq={Seq},IsCrcChecked={crcChecked}");
            }
            Payload = new byte[BodyLength];
            return true;
        }

        /// <summary>
        /// 校验 Payload MD5
        /// </summary>
        public bool OnParsingBody(ReadOnlySpan<byte> body)
        {
            if (Payload == null || body.Length != Payload.Length)
                return false;

            if (BodyLength == 0)
            {
                return true;
            }

            body.CopyTo(Payload);

            using var md5 = System.Security.Cryptography.MD5.Create();
            var actualMd5 = md5.ComputeHash(Payload);
            IsMd5Right = actualMd5.AsSpan().SequenceEqual(Md5);
            Console.WriteLine($"Recive Header: Seq={Seq},IsMd5Right={IsMd5Right},Length={body.Length}");
            return true;
        }
    }

    public class MyCustomAdapter : CustomFixedHeaderDataHandlingAdapter<DataRequestInfo>
    {
        public override bool CanSendRequestInfo => true;
        private byte[] HeaderCache;

        public override int HeaderLength => 69;

        public MyCustomAdapter()
        {
#if !DEBUG
            this.CacheTimeoutEnable = true;
            this.CacheTimeout = TimeSpan.FromSeconds(1);
            this.UpdateCacheTimeWhenRev = true;
#endif
        }

        protected override DataRequestInfo GetInstance()
        {
            return new DataRequestInfo();
        }
    }

    public class MyServerPlugin : PluginBase, ITcpReceivedPlugin
    {
        public Task OnTcpReceived(ITcpSession client, ReceivedDataEventArgs e)
        {
            if (e.RequestInfo is DataRequestInfo packet)
            {
                Console.WriteLine($"Received Seq={packet.Seq}, Size={packet.Payload.Length}");
                var ack = DataRequestInfo.Build(packet.Seq, 0x01, Array.Empty<byte>());
            }
            return Task.CompletedTask;
        }
    }
}
