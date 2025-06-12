using MemoryPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TouchSocket.Core;

[MemoryPackable]
[GeneratorPackage]
public partial class SomeData : PackageBase
{
    public int IntValue { get; set; }
    public string StrValue { get; set; }
    public long LongValue { get; set; }
}
