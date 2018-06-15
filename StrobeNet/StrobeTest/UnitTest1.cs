using System;
using Xunit;
using StrobeNet;

namespace StrobeTest
{
    using System.Text;

    public class Operation
    {
        public string OpName;

        public string OpCustomString;

        public int OpSecurity;

        public string OpStateAfter;
    }

    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            // start the run
            var testVectorName = "simple tests";
            // init
            var (s, op) = DebugInit("custom string", 128);
        }

        [Fact]
        public void TestStream2()
        {
            var s = new Strobe("custom string number 2, that's a pretty long string", 128);
            var key = new byte[] { 0,1,0,1,0,1,0,1,0,0,1,0,0,1,0,1,0,1,0,1,0,1,0,1,0,1,0,1,0,0,1,0,0,1 };
            s.Operate(false, "KEY", key, 0, false);
            s.Operate(false, "KEY", key, 0, true);
            var message = Encoding.UTF8.GetBytes(("hello, how are you good sir? ????"));
            s.Operate(false, "AD", message, 0, false);
            s.Operate(false, "AD", message, 0, true);
            s.Operate(false, "AD", message, 0, false);
            Console.WriteLine(s.debugPrintState());
        }

        [Fact]
        public void TestStream2Py()
        {
            var s = new StrobePy(Encoding.UTF8.GetBytes("custom string number 2, that's a pretty long string"), 128);
            var key = new byte[] { 0, 1, 0, 1, 0, 1, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 0, 1, 0, 0, 1 };
            s.Operate("KEY", key, false);
            s.Operate("KEY", key, true);
            var message = Encoding.UTF8.GetBytes(("hello, how are you good sir? ????"));
            s.Operate("AD", message, false);
            s.Operate("AD", message, true);
            var r = s.Operate("AD", message, false);
            Console.WriteLine(BitConverter.ToString(r).Replace("-", ""));
            Console.WriteLine(s.DebugPrintState());
        }


        public Tuple<Strobe, Operation> DebugInit(string customString, int security)
        {
            var s = new Strobe(customString, security);
            var op = new Operation();
            op.OpName = "init";
            op.OpCustomString = customString;
            op.OpSecurity = 128;
            op.OpStateAfter = s.debugPrintState();

            return new Tuple<Strobe, Operation>(s, op);
        }
    }
}
