#nullable enable

//#if !NETSTANDARD2_0 && !UNITY_2019_1_OR_NEWER

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Cysharp.Collections
{
    public static class NativeMemoryArrayExtensions
    {
        public static async Task ReadFromAsync(this NativeMemoryArray<byte> buffer, Stream stream, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
        {
            var writer = buffer.CreateBufferWriter();

            int read;
            while ((read = await stream.ReadAsync(writer.GetMemory(), cancellationToken).ConfigureAwait(false)) != 0)
            {
                progress?.Report(read);
                writer.Advance(read);
            }
        }

        public static async Task WriteToFileAsync(this NativeMemoryArray<byte> buffer, string path, FileMode mode = FileMode.Create, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
        {
            using (var fs = new FileStream(path, mode, FileAccess.Write, FileShare.ReadWrite, 1, useAsync: true))
            {
                await buffer.WriteToAsync(fs, progress: progress, cancellationToken: cancellationToken);
            }
        }

        public static async Task WriteToAsync(this NativeMemoryArray<byte> buffer, Stream stream, int chunkSize = int.MaxValue, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
        {
            foreach (var item in buffer.AsReadOnlyMemoryList(chunkSize))
            {
                await stream.WriteAsync(item, cancellationToken);
                progress?.Report(item.Length);
            }
        }

        public static List<Stream> SpliteToStreamList(this NativeMemoryArray<byte> buffer, long chunkSize = int.MaxValue)
        {
            int partCount = (int)Math.Ceiling((double)buffer.Length / chunkSize);
            List<Stream> streams = new List<Stream>(partCount);

            for (int i = 0; i < partCount; i++)
            {
                long length = chunkSize;
                if ((i + 1) * chunkSize > buffer.Length)
                {
                    length = buffer.Length - i * chunkSize;
                }
                var stream = buffer.AsStream(i * chunkSize, length);
                streams.Add(stream);
            }

            return streams;
        }

        public unsafe static T* GetPointer<T>(this NativeMemoryArray<T> arr) where T : unmanaged
        {
            if (arr.Length == 0)
            {
                return default;
            }
            return (T*)Unsafe.AsPointer<T>(ref arr[0]);
        }
    }
}

//#endif