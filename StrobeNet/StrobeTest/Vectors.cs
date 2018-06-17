namespace StrobeTest
{
    using System.Collections.Generic;

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
            public string Name { get; set; }

            public string CustomString { get; set; }

            public int Security { get; set; }

            public bool? Meta { get; set; }

            public string StateAfter { get; set; }

            public bool? Stream { get; set; }

            public string InputData { get; set; }

            public string Output { get; set; }

            public int InputLength { get; set; }
        }
    }
}