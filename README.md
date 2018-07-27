# StrobeNet

[![Build Status](https://travis-ci.org/Fasjeit/StrobeNet.svg?branch=master)](https://travis-ci.org/Fasjeit/StrobeNet)

This repository contains an .NET implementation of the [Strobe protocol framework](https://strobe.sourceforge.io/). See [this David Wong's blogpost](https://www.cryptologie.net/article/416/the-strobe-protocol-framework/) for an explanation of what is the framework.

**The implementation of Strobe has not been thoroughly tested. Do not use this in production**.

This implementation is adaptation of [StrobeGo by Davin Wong](https://github.com/mimoo/StrobeGo) and [Strobe python reference code](https://sourceforge.net/p/strobe/code) for .NET platform.

## Install

Install [NuGet packet](https://www.nuget.org/packages/StrobeNet/):

dotnet core
```
dotnet add package StrobeNet
```

.NET Framework
```
Install-Package StrobeNet
```

## Usage

See [Wiki](https://github.com/Fasjeit/StrobeNet/wiki) for documentation. Here is an example usage:

```C#
namespace Samples
{
    using System;
    using System.Text;

    using StrobeNet;

    class Program
    {
        static void Main()
        {
            // Create strobe object, setting init string and security
            var strobe = new Strobe("MyStrobe", 128);

            var messageByte = Encoding.ASCII.GetBytes("Hello gentlemen");
            strobe.Ad(false, messageByte);

            var prfBytes = strobe.Prf(16);
            Console.WriteLine(BitConverter.ToString(prfBytes).Replace("-", ""));
        }
    }
}
```

## Roadmap

* xml on operation enum
* Implement API test
* Samples
* StreamBuffer using
