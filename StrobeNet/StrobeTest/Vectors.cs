namespace StrobeTest
{
    using System;
    using System.Collections.Generic;

    using StrobeNet;

    internal class Vectors
    {
        public List<TestVector> TestVectors { get; set; }
    }

    internal class TestVector
    {
        public string Name { get; set; }

        public List<Operation> Operations { get; set; }

        internal class Operation
        {
            private readonly Dictionary<string, Strobe.Operation> vectorOperationMap =
                new Dictionary<string, Strobe.Operation>()
                    {
                        { "AD", Strobe.Operation.Ad },
                        { "KEY", Strobe.Operation.Key },
                        { "PRF", Strobe.Operation.Prf },
                        { "send_CLR", Strobe.Operation.SendClr },
                        { "recv_CLR", Strobe.Operation.RecvClr },
                        { "send_ENC", Strobe.Operation.SendEnc },
                        { "recv_ENC", Strobe.Operation.RecvEnc },
                        { "send_MAC", Strobe.Operation.SendMac },
                        { "recv_MAC", Strobe.Operation.RecvMac },
                        { "RATCHET", Strobe.Operation.Ratchet }
                    };


            public string Name { get; set; }

            public string CustomString { get; set; }

            public int Security { get; set; }

            public bool? Meta { get; set; }

            public string StateAfter { get; set; }

            public bool? Stream { get; set; }

            public string InputData { get; set; }

            public string Output { get; set; }

            public int InputLength { get; set; }

            public Strobe.Operation GetStrobeOperation()
            {
                if (!this.vectorOperationMap.TryGetValue(this.Name, out var operation))
                {
                    throw new Exception($"Not a valid operation: [{this.Name}]");
                }

                return operation;
            }
        }
    }
}