using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common;

public class ImageMemoryPool
{
    public static bool EnablePool { get; set; }
    public static Dictionary<string, ImageMemoryPool> Instances = new Dictionary<string, ImageMemoryPool>();
    public static Dictionary<string, Task> Tasks = new Dictionary<string, Task>();
    public long ImageSize { get; private set; }
    List<ImageMemoryByteArray> pool;
    public int PoolCount => pool.Count;
    public PeriodicTimer Timer;

    public ImageMemoryPool(long imageSize, string type)
    {
        this.ImageSize = imageSize;
        pool = new List<ImageMemoryByteArray>();
        for (int i = 0; i < 2; i++)
        {
            var image = new ImageMemoryByteArray(imageSize);
            pool.Add(image);
        }
        Timer = new PeriodicTimer(new TimeSpan(0, 1, 0));
        Instances.Add(type, this);


    }

    public static ImageMemoryPool GetInstance(string type)
    {
        if (EnablePool && Instances.TryGetValue(type, out ImageMemoryPool pool))
        {
            return pool;
        }
        else
        {
            return null;
        }
    }


    public ImageMemoryByteArray Get()
    {
        if (EnablePool)
        {
            lock (pool)
            {
                foreach (var item in pool)
                {
                    if (item.Refence == 0)
                    {
                        item.IncrementRefrence();
                        return item;
                    }
                }

                if (pool.Count < 4)//上限
                {
                    var newitem = new ImageMemoryByteArray(ImageSize);
                    pool.Add(newitem);
                    return newitem;
                }
                else
                {
                    throw new Exception("图片池超过上限");
                }
            }
        }
        else
        {
            var newitem = new ImageMemoryByteArray(ImageSize);
            return newitem;
        }
    }

    private async Task AutoCollect()
    {
        while (await Timer.WaitForNextTickAsync())
        {
            lock (pool)
            {
                int free = 0;
                List<ImageMemoryByteArray> Remove = new List<ImageMemoryByteArray>();
                foreach (var item in pool)
                {
                    if (item.Refence == 0)
                    {
                        Remove.Add(item);
                        free++;
                    }
                }
                pool.RemoveAll(Remove.Contains);
            }
        }
    }
}
