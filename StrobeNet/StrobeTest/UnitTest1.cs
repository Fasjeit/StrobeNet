using System;
using Xunit;
using StrobeNet;

namespace StrobeTest
{
    using System.Text;

    public class FunctionalityUnitTest
    {
        [Fact]
        public void TestClone()
        {
            var message = "hello, how are you good sir?";

            var s1 = new Strobe("myHash", 128);
            var s2 = s1.Clone() as Strobe;

            s1.Operate(false, "AD", Encoding.ASCII.GetBytes(message), 0, false);
            var state1 = s1.DebugPrintState();
            var out1 = BitConverter.ToString(s1.Prf(32)).Replace("-", "");
            var state12 = s1.DebugPrintState();

            s2.Operate(false, "AD", Encoding.ASCII.GetBytes(message), 0, false);
            var state2 = s2.DebugPrintState();
            var out2 = BitConverter.ToString(s2.Prf(32)).Replace("-", "");
            var state22 = s2.DebugPrintState();

            System.Diagnostics.Debug.WriteLine(out1);
            System.Diagnostics.Debug.WriteLine(out2);

            if (out1 != out2)
            {
                throw new Exception("strobe cannot stream correctly");
            }
        }

        [Fact]
        public void TestStream()
        {
            var message1 = "hello";
            var message2 = "how are you good sir?";
            var fullmessage = message1 + message2;

            var s1 = new Strobe("myHash", 128);
            var s2 = s1.Clone() as Strobe;

            s1.Operate(false, "AD", Encoding.ASCII.GetBytes(fullmessage), 0, false);
            var out1 =  BitConverter.ToString(s1.Prf(32)).Replace("-", "");

            s2.Operate(false, "AD", Encoding.ASCII.GetBytes(message1), 0, false);
            s2.Operate(false, "AD", Encoding.ASCII.GetBytes(message2), 0, true);
            var out2 = BitConverter.ToString(s2.Prf(32)).Replace("-", "");

            System.Diagnostics.Debug.WriteLine(out1);
            System.Diagnostics.Debug.WriteLine(out2);

            if (out1 != out2)
            {
                throw new Exception("strobe cannot stream correctly");
            }
        }

        [Fact]
        public void TestStream2()
        {
            var s = new Strobe("custom string number 2, that's a pretty long string", 128);
            System.Diagnostics.Debug.WriteLine(s.DebugPrintState());
            var key = Encoding.ASCII.GetBytes("0101010100100101010101010101001001");
            System.Diagnostics.Debug.WriteLine(s.DebugPrintState());
            s.Operate(false, "KEY", key, 0, false);
            System.Diagnostics.Debug.WriteLine(s.DebugPrintState());
            s.Operate(false, "KEY", key, 0, true);
            System.Diagnostics.Debug.WriteLine(s.DebugPrintState());
            var message = Encoding.ASCII.GetBytes(("hello, how are you good sir? ????"));
            s.Operate(false, "AD", message, 0, false);
            System.Diagnostics.Debug.WriteLine(s.DebugPrintState());
            s.Operate(false, "AD", message, 0, true);
            System.Diagnostics.Debug.WriteLine(s.DebugPrintState());
            s.Operate(false, "AD", message, 0, false);
            System.Diagnostics.Debug.WriteLine(s.DebugPrintState());

            if (!string.Equals(s.DebugPrintState(), "5117b46c2d842655c1be2a69f64f16aaaad2c0050fe2ac5446afe44345a9b10d0"
                + "44c8b3ec8005a9e362c0a431ab5c4d8228c2f890ae56ad3fef4404aa6cc76704b503d627553ae9635d329c"
                + "dfa86ed29ec0dd79787ff3fcefdee7463c053ef3b4a4fa7c8eb89a6372df2c4ccfc7469d7447bd19a67940"
                + "642334706e5ff6b1ef58514e55c6b5c6921c58eb7cb5c57978c92c42e598926fcfdcd9705fb948ed6fe902"
                + "7c65fb0659c98a9c9668d523dfa2b27bde76224944503b686901c989fedac34994dd16daedf00", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new Exception("this is not working");
            }
        }

        //[Fact]
        //public void TestStream2Py()
        //{
        //    var s = new StrobePy(Encoding.UTF8.GetBytes("custom string number 2, that's a pretty long string"), 128);
        //    var key = new byte[] { 0, 1, 0, 1, 0, 1, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 0, 1, 0, 0, 1 };
        //    s.Operate("KEY", key, false);
        //    s.Operate("KEY", key, true);
        //    var message = Encoding.UTF8.GetBytes(("hello, how are you good sir? ????"));
        //    s.Operate("AD", message, false);
        //    s.Operate("AD", message, true);
        //    var r = s.Operate("AD", message, false);
        //    Console.WriteLine(BitConverter.ToString(r).Replace("-", ""));
        //    Console.WriteLine(s.DebugPrintState());
        //}


        //public Tuple<Strobe, Operation> DebugInit(string customString, int security)
        //{
        //    var s = new Strobe(customString, security);
        //    var op = new Operation();
        //    op.OpName = "init";
        //    op.OpCustomString = customString;
        //    op.OpSecurity = 128;
        //    op.OpStateAfter = s.DebugPrintState();

        //    return new Tuple<Strobe, Operation>(s, op);
        //}
    }
}
