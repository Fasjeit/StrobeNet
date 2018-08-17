namespace StrobeNet.Tests
{
    using System;
    using System.Collections.Generic;

    using StrobeNet.Enums;

    internal class Vectors
    {
        public List<TestVector> TestVectors { get; set; }
    }

    internal class TestVector
    {
        public string Name { get; set; }

        public List<OperationProperties> Operations { get; set; }

        internal class OperationProperties
        {
            private readonly Dictionary<string, Operation> vectorOperationMap =
                new Dictionary<string, Operation>()
                    {
                        { "AD", Operation.Ad },
                        { "KEY", Operation.Key },
                        { "PRF", Operation.Prf },
                        { "send_CLR", Operation.SendClr },
                        { "recv_CLR", Operation.RecvClr },
                        { "send_ENC", Operation.SendEnc },
                        { "recv_ENC", Operation.RecvEnc },
                        { "send_MAC", Operation.SendMac },
                        { "recv_MAC", Operation.RecvMac },
                        { "RATCHET", Operation.Ratchet }
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

            public Operation GetStrobeOperation()
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