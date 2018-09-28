namespace Benchmark
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;

    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Running;

    [RPlotExporter, RankColumn]
    public class TheEasiestBenchmark
    {
        //[Params(64, 128, 256, 512, 1024, 4094, 1048576)]
        //public int N;

        private byte[] bytearray;

        private ulong[] ulongArary;

        [Benchmark(Description = "OldByteToUint")]
        public void OldByteToUint()
        {
            var result = new ulong[this.bytearray.Length / 8];
            for (int i = 0; i < this.bytearray.Length; i += 8)
            {
                result[i / 8] = BitConverter.ToUInt64(this.bytearray, i);
            }

            this.ulongArary = result;
        }

        [Benchmark(Description = "NewByteToUint")]
        public void NewByteToUint()
        {
            ulong[] decoded = new ulong[this.bytearray.Length / 8];
            Buffer.BlockCopy(this.bytearray, 0, decoded, 0, this.bytearray.Length);
            this.ulongArary = decoded;
        }

        [Benchmark(Description = "UnsafeByteToUint")]
        public unsafe void  UnsafeByteToUint()
        {
            unsafe
            {
                fixed (ulong* src = this.ulongArary)
                {
                    Marshal.Copy(new IntPtr((void*)src), this.bytearray, 0, 200);
                }
            }
        }

        [Benchmark(Description = "UnsafeUintToByte2")]
        public unsafe void UnsafeUintToByte2()
        {
            unsafe
            {
                fixed (ulong* src = this.ulongArary)
                {
                    fixed (byte* dst = this.bytearray)
                    {
                        ulong* pl = (ulong*)dst;
                        for (int i = 0; i < this.ulongArary.Length; i++)
                        {
                            *(pl + i) = *(src + i);
                        }
                    }
                }
            }
        }

        [Benchmark(Description = "UnsafeByteToUlong2")]
        public unsafe void UnsafeByteToUlong2()
        {
            unsafe
            {
                fixed (byte* src = this.bytearray)
                {
                    fixed (ulong* dst = this.ulongArary)
                    {
                        byte* pl = (byte*)dst;
                        for (int i = 0; i < this.bytearray.Length; i++)
                        {
                            *(pl + i) = *(src + i);
                        }
                    }
                }
            }
        }

        [GlobalCleanup]
        public void cleanUp()
        {
        }

        [GlobalSetup]
        public void Setup()
        {
            this.bytearray = new byte[200];
            this.ulongArary = new ulong[25];

            var rng = new RNGCryptoServiceProvider();
            rng.GetBytes(this.bytearray);
        }
    }



    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<TheEasiestBenchmark>();
        }
    }
}
