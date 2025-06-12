using Cysharp.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ImageMemoryByteArray : NativeMemoryArray<byte>
{
    int refrence;
    public int Refence => refrence;

    public void IncrementRefrence()
    {
        Interlocked.Increment(ref refrence);
    }

    public void DecrementRefrence()
    {
        Interlocked.Decrement(ref refrence);
    }

    public long ActualLength { get; set; }

    public ImageMemoryByteArray(long length, bool skipZeroClear = false, bool addMemoryPressure = false) :
        base(length, skipZeroClear, addMemoryPressure)
    {
    }
}
