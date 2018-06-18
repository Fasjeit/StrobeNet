namespace StrobeTest
{
    using System;
    using System.IO;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    using StrobeNet;
    using StrobeNet.Extensions;

    using Xunit;

    public class VectorsTest
    {
        private const string VectorFile = "TestVectors.json";

        private Vectors testVectors;

        private Vectors TestVectors
        {
            get
            {
                if (this.testVectors == null)
                {
                    // snake_case resolver
                    var contractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() };

                    // Reading json
                    var testData = File.ReadAllText(VectorsTest.VectorFile);
                    this.testVectors = JsonConvert.DeserializeObject<Vectors>(
                        testData,
                        new JsonSerializerSettings { ContractResolver = contractResolver });
                }
                return this.testVectors;
            }
        }

        [Fact]
        public void StreamingTest()
        {
            this.RunTestVector("streaming tests");
        }

        [Fact]
        public void BoundaryTest()
        {
            this.RunTestVector("boundary tests");
        }

        [Fact]
        public void MetaTests()
        {
            this.RunTestVector("meta tests");
        }

        [Fact]
        public void SimpleTests()
        {
            this.RunTestVector("simple tests");
        }

        private void RunTestVector(string vectorName)
        {
            var vector = this.TestVectors.TestVectors.Find(tv => tv.Name == vectorName);

            Strobe strobe = null;

            foreach (var operation in vector.Operations)
            {
                if (operation.Name == "init")
                {
                    strobe = new Strobe(operation.CustomString, operation.Security);
                    if (!string.Equals(
                            strobe.DebugPrintState(),
                            operation.StateAfter,
                            StringComparison.InvariantCultureIgnoreCase))
                    {
                        throw new Exception();
                    }

                    continue;
                }

                if (strobe == null)
                {
                    throw new ArgumentNullException(nameof(strobe));
                }

                var result = strobe.Operate(
                    operation.Meta ?? false,
                    operation.GetStrobeOperation(),
                    operation.InputData?.ToByteArray(),
                    operation.InputLength,
                    operation.Stream ?? false);
                if (!string.Equals(
                        strobe.DebugPrintState(),
                        operation.StateAfter,
                        StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new Exception();
                }

                if (operation.Output != null && !string.Equals(
                        operation.Output,
                        result.ToHexString(),
                        StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new Exception();
                }
            }
        }
    }
}