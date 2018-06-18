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

            // Authenticating message
            var messageByte = Encoding.ASCII.GetBytes("Hello gentlemens");
            strobe.Ad(false, messageByte);

            // Get Prf output
            var prfBytes = strobe.Prf(16);
            Console.WriteLine(BitConverter.ToString(prfBytes).Replace("-", ""));
        }
    }
}
