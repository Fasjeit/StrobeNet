namespace StrobeNet.Tests
{
    using System;
    using System.Text;

    using StrobeNet;
    using StrobeNet.Enums;
    using StrobeNet.Extensions;

    using Xunit;

    public class FunctionalityUnitTest
    {
        [Fact]
        public void TestClone()
        {
            var message = "hello, how are you good sir?";

            var s1 = new Strobe("myHash", 128);
            var s2 = s1.Clone() as Strobe;

            s1.Operate(false, Operation.Ad, Encoding.ASCII.GetBytes(message), 0, false);
            var state1 = s1.DebugPrintState();
            var out1 = s1.Prf(32).ToHexString();
            var state12 = s1.DebugPrintState();

            s2.Operate(false, Operation.Ad, Encoding.ASCII.GetBytes(message), 0, false);
            var state2 = s2.DebugPrintState();
            var out2 = s2.Prf(32).ToHexString();
            var state22 = s2.DebugPrintState();

            System.Diagnostics.Debug.WriteLine(out1);
            System.Diagnostics.Debug.WriteLine(out2);

            if (out1 != out2 || state1 != state2 || state12 != state22)
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

            s1.Operate(false, Operation.Ad, Encoding.ASCII.GetBytes(fullmessage), 0, false);
            var out1 = s1.Prf(32).ToHexString();

            s2.Operate(false, Operation.Ad, Encoding.ASCII.GetBytes(message1), 0, false);
            s2.Operate(false, Operation.Ad, Encoding.ASCII.GetBytes(message2), 0, true);
            var out2 = s2.Prf(32).ToHexString();

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
            s.Operate(false, Operation.Key, key, 0, false);
            System.Diagnostics.Debug.WriteLine(s.DebugPrintState());
            s.Operate(false, Operation.Key, key, 0, true);
            System.Diagnostics.Debug.WriteLine(s.DebugPrintState());
            var message = Encoding.ASCII.GetBytes(("hello, how are you good sir? ????"));
            s.Operate(false, Operation.Ad, message, 0, false);
            System.Diagnostics.Debug.WriteLine(s.DebugPrintState());
            s.Operate(false, Operation.Ad, message, 0, true);
            System.Diagnostics.Debug.WriteLine(s.DebugPrintState());
            s.Operate(false, Operation.Ad, message, 0, false);
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
    }
}
