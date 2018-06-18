namespace StrobeNet
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using StrobeNet.Extensions;

    public class Strobe : ICloneable
    {
        /// <summary>
        /// The size of the authentication tag used in AEAD functions
        /// </summary>
        private const int MacLen = 16;

        /// <summary>
        /// Used to avoid padding during the first permutation
        /// </summary>
        private readonly bool initialized;

        /// <summary>
        /// Operation - flag map
        /// </summary>
        private readonly Dictionary<Operation, Flag> operationMap =
            new Dictionary<Operation, Flag>
                {
                    { Operation.Ad, Flag.FlagA },
                    { Operation.Key, Flag.FlagA | Flag.FlagC },
                    { Operation.Prf, Flag.FlagI | Flag.FlagA | Flag.FlagC },
                    { Operation.SendClr, Flag.FlagA | Flag.FlagT },
                    { Operation.RecvClr, Flag.FlagI | Flag.FlagA | Flag.FlagT },
                    { Operation.SendEnc, Flag.FlagA | Flag.FlagC | Flag.FlagT },
                    { Operation.RecvEnc, Flag.FlagI | Flag.FlagA | Flag.FlagC | Flag.FlagT },
                    { Operation.SendMac, Flag.FlagC | Flag.FlagT },
                    { Operation.RecvMac, Flag.FlagI | Flag.FlagC | Flag.FlagT },
                    { Operation.Ratchet, Flag.FlagC }
                };

        /// <summary>
        /// Strobe R param
        /// </summary>
        private readonly int strobeR; // duplexRate - 2

        /// <summary>
        /// Streaming API
        /// </summary>
        private Flag curFlags;

        /// <summary>
        /// Role
        /// </summary>
        private Role i0;

        /// <summary>
        /// Curent position in storage
        /// </summary>
        private byte pos;

        /// <summary>
        /// Start of the current operation
        /// </summary>
        private byte posBegin;

        /// <summary>
        /// Current state
        /// </summary>
        private byte[] state = new byte[25 * 8];

        /// <summary>
        /// Get current state
        /// </summary>
        internal ulong[] GetUint64State => Keccak.TransformArray(this.state);

        /// <summary>
        /// Initialize a new strobe instance
        /// </summary>
        /// <param name="customizationString">customization string may be empty</param>
        /// <param name="security">ecurity target (either 128 or 256)</param>
        public Strobe(string customizationString, int security)
        {
            // compute security and rate
            if (security != 128 && security != 256)
            {
                throw new Exception("strobe: security must be set to either 128 or 256");
            }

            var duplexRate = 1600 / 8 - security / 4;
            this.strobeR = duplexRate - 2;

            this.i0 = Role.None;
            this.initialized = false;

            // absorb domain + initialize + absorb custom string
            var domain = new byte[] { 1, (byte)(this.strobeR + 2), 1, 0, 1, 12 * 8 };
            domain = domain.Concat(Encoding.UTF8.GetBytes("STROBEv1.0.2")).ToArray();

            this.Duplex(domain, false, false, true);

            this.initialized = true;
            this.Operate(true, Operation.Ad, Encoding.UTF8.GetBytes(customizationString), 0, false);
        }

        /// <summary>
        /// Create Strobe instance for clone copy
        /// </summary>
        /// <param name="strobeR"></param>
        private Strobe(int strobeR)
        {
            this.strobeR = strobeR;
            this.initialized = true;
        }


        /// <summary>
        /// Insert a key into the state
        /// Provides forward secrecy.
        /// </summary>
        /// <param name="key">
        /// Key to be added
        /// </param>
        public void Key(byte[] key)
        {
            this.Operate(false, Operation.Key, key, 0, false);
        }

        /// <summary>
        /// PRF provides a hash of all previous operations
        /// It can also be used to generate random numbers, it is forward secure.
        /// </summary>
        /// <param name="outputLen">
        /// Expected output length
        /// </param>
        public byte[] Prf(int outputLen)
        {
            return this.Operate(false, Operation.Prf, null, outputLen, false);
        }

        /// <summary>
        /// Encrypt plaintext.
        /// Should be followed by Send_MAC in order to protect its integrity
        /// </summary>
        /// <param name="meta">
        /// Framing data.
        /// </param>
        /// <param name="plaintext">
        /// Plaintext to be encrypted
        /// </param>
        public byte[] SendEncUnauthenticated(bool meta, byte[] plaintext)
        {
            return this.Operate(meta, Operation.SendEnc, plaintext, 0, false);
        }

        ///  <summary>
        ///  Decrypt some received ciphertext.
        ///  it should be followed by Recv_MAC in order to protect its integrity
        ///  </summary>
        ///  <param name="meta">
        ///  Framing data.
        ///  </param>
        /// <param name="ciphertext">
        /// Ciphertext to be decrypted
        /// </param>
        public byte[] RecvEncUnauthenticated(bool meta, byte[] ciphertext)
        {
            return this.Operate(meta, Operation.RecvEnc, ciphertext, 0, false);
        }

        /// <summary>
        /// Authenticate Additional Data
        /// Should be followed by a Send_MAC or Recv_MAC in order to truly work
        /// </summary>
        /// <param name="meta">
        /// Framing data.
        /// </param>
        /// <param name="additionalData">
        /// Data to authenticate
        /// </param>
        public void Ad(bool meta, byte[] additionalData)
        {
            this.Operate(meta, Operation.Ad, additionalData, 0, false);
        }

        /// <summary>
        /// Send data in cleartext
        /// </summary>
        /// <param name="meta">
        /// Framing data.
        /// </param>
        /// <param name="cleartext">
        /// Cleartext to send
        /// </param>
        public byte[] SendClr(bool meta, byte[] cleartext)
        {
            return this.Operate(meta, Operation.SendClr, cleartext, 0, false);
        }

        /// <summary>
        /// Produce an authentication tag.
        /// </summary>
        /// <param name="meta">
        /// Framing data.
        /// </param>
        /// <param name="outputLength">
        /// Expected tag length
        /// </param>
        public byte[] SendMac(bool meta, int outputLength)
        {
            return this.Operate(meta, Operation.SendMac, null, outputLength, false);
        }

        /// <summary>
        /// Allows you to verify a received authentication tag.
        /// </summary>
        /// <param name="meta">
        /// Framing data.
        /// </param>
        /// <param name="mac">
        /// Tag to verufy
        /// </param>
        public bool RecvMac(bool meta, byte[] mac)
        {
            return this.Operate(meta, Operation.RecvMac, mac, 0, false)[0] == 0;
        }
        /// <summary>
        /// Introduce forward secrecy in a protocol.
        /// </summary>
        /// <param name="length">
        /// Expected length
        /// </param>
        public void Ratchet(int length)
        {
            this.Operate(false, Operation.Ratchet, null, length, false);
        }

        /// <summary>
        /// Encrypt data and authenticate additional data
        /// </summary>
        /// <param name="plaintext">
        /// Data to be encrypted and authenticated
        /// </param>
        /// <param name="ad">
        /// Additinal data to be authenticated
        /// </param>
        public byte[] SendAead(byte[] plaintext, byte[] ad)
        {
            var ciphertext = this.SendEncUnauthenticated(false, plaintext);
            this.Ad(false, ad);
            ciphertext = ciphertext.Concat(this.SendMac(false, Strobe.MacLen)).ToArray();
            return ciphertext;
        }

        /// <summary>
        /// Decrypt data and authenticate additional data
        /// It is similar to AES-GCM.
        /// </summary>
        /// <param name="ciphertext">
        /// Ciphertext to be verified and decrypted 
        /// </param>
        /// <param name="ad">
        /// Additinal auth data to be verified
        /// </param>
        /// <param name="plaintext">
        /// Resulting plaintext
        /// </param>
        public bool RecvAead(byte[] ciphertext, byte[] ad, out byte[] plaintext)
        {
            if (ciphertext.Length < Strobe.MacLen)
            {
                plaintext = null;
                return false;
            }

            var messageLength = ciphertext.Length - Strobe.MacLen;
            plaintext = this.RecvEncUnauthenticated(false, ciphertext.Take(messageLength).ToArray());

            this.Ad(false, ad);
            return this.RecvMac(false, ciphertext.Skip(messageLength).ToArray());
        }

        /// <summary>
        /// Get clone of curent object
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            var result = new Strobe(this.strobeR)
                             {
                                 curFlags = this.curFlags,
                                 i0 = this.i0,
                                 pos = this.pos,
                                 posBegin = this.posBegin,
                             };
            Array.Copy(this.state, 0, result.state, 0, this.state.Length);
            return result;
        }

        /// <summary>
        /// Operate runs an operation
        /// For operations that only require a length, provide the length via the
        /// length argument. For other operations provide a zero length.
        /// Result is always retrieved through the return value. For boolean results,
        /// check that the first index is 0 for true, 1 for false.
        /// </summary>
        public byte[] Operate(bool meta, Operation operation, byte[] dataConst, int length, bool more)
        {
            // operation is valid?
            if (!this.operationMap.TryGetValue(operation, out var flags))
            {
                throw new Exception($"Not a valid operation: [{operation}]");
            }

            // operation is meta?
            if (meta)
            {
                flags |= Flag.FlagM;
            }

            // does the operation requires a length?
            byte[] data;

            if ((flags & (Flag.FlagI | Flag.FlagT)) != (Flag.FlagI | Flag.FlagT)
                && (flags & (Flag.FlagI | Flag.FlagA)) != Flag.FlagA)
            {
                if (length == 0)
                {
                    throw new Exception("A length should be set for this operation");
                }

                data = new byte[length];
            }
            else
            {
                if (length != 0)
                {
                    throw new Exception("Output length must be zero except for PRF, send_MAC and RATCHET operations");
                }

                data = dataConst;
            }

            if (more)
            {
                if (flags != this.curFlags)
                {
                    throw new Exception("Flag should be the same when streaming operations");
                }
            }
            else
            {
                this.BeginOp(flags);
                this.curFlags = flags;
            }

            // Operation

            var cAfter = (flags & (Flag.FlagC | Flag.FlagI | Flag.FlagT)) == (Flag.FlagC | Flag.FlagT);
            var cBefore = (flags & Flag.FlagC) != 0 && !cAfter;

            var processed = this.Duplex(data, cBefore, cAfter, false);

            if ((flags & (Flag.FlagI | Flag.FlagA)) == (Flag.FlagI | Flag.FlagA))
            {
                return processed;
            }

            if ((flags & (Flag.FlagI | Flag.FlagT)) == Flag.FlagT)
            {
                // Return data for the transport.
                return processed;
            }

            if ((flags & (Flag.FlagI | Flag.FlagA | Flag.FlagT)) == (Flag.FlagI | Flag.FlagT))
            {
                // Check MAC: all output bytes must be 0
                if (more)
                {
                    throw new Exception("not supposed to check a MAC with the 'more' streaming option");
                }

                byte failures = 0;
                foreach (var dataByte in processed)
                {
                    failures |= dataByte;
                }

                return new[] { failures }; // 0 if correct, 1 if not
            }

            // Operation has no output
            return null;
        }

        // beginOp: starts an operation
        private void BeginOp(Flag flags)
        {
            if ((flags & Flag.FlagT) != 0)
            {
                if (this.i0 == Role.None)
                {
                    this.i0 = (Role)(flags & Flag.FlagI);
                }

                flags ^= (Flag)this.i0;
            }

            var oldBegin = this.posBegin;
            this.posBegin = (byte)(this.pos + 1);
            var forceF = (flags & (Flag.FlagC | Flag.FlagK)) != 0;

            this.Duplex(new[] { oldBegin, (byte)flags }, false, false, forceF);
        }

        private byte[] Duplex(byte[] data, bool cbefore, bool cafter, bool forceF)
        {
            if (cbefore && cafter)
            {
                throw new Exception($"either {nameof(cbefore)} or {nameof(cafter)} should be set to false");
            }

            // Copy data
            var newData = (byte[])data.Clone();

            for (var i = 0; i < newData.Length; i++)
            {
                // Process data block by block
                if (cbefore)
                {
                    newData[i] ^= this.state[this.pos];
                }

                this.state[this.pos] ^= newData[i];
                if (cafter)
                {
                    newData[i] = this.state[this.pos];
                }

                this.pos += 1;
                if (this.pos == this.strobeR)
                {
                    this.RunF();
                }
            }

            // sometimes we the next operation to start on a new block
            if (forceF && this.pos != 0)
            {
                this.RunF();
            }

            return newData;
        }

        /// <summary>
        /// STROBE's + cSHAKE's padding and the Keccak permutation
        /// </summary>
        private void RunF()
        {
            if (this.initialized)
            {
                this.state[this.pos] ^= this.posBegin;
                this.state[this.pos + 1] ^= 0x04;
                this.state[this.strobeR + 1] ^= 0x80;
            }

            // run the permutation
            Keccak.KeccakF1600(ref this.state, 24);
            this.pos = this.posBegin = 0;
        }

        /// <summary>
        /// Print current state
        /// </summary>
        /// <returns></returns>
        public string DebugPrintState()
        {
            return this.state.ToHexString();
        }

        public enum Operation
        {
            Ad,
            Key,
            Prf,
            SendClr,
            RecvClr,
            SendEnc,
            RecvEnc,
            SendMac,
            RecvMac,
            Ratchet,
        }

        private enum Role
        {
            /// <summary>
            /// Set if we send the first transport message
            /// </summary>
            Initiator = 0,

            /// <summary>
            /// Set if we receive the first transport message
            /// </summary>
            Responder = 1,

            /// <summary>
            /// starting value
            /// </summary>
            None = 2,
        }

        [Flags]
        private enum Flag
        {
            FlagI = 1,

            FlagA = 2,

            FlagC = 4,

            FlagT = 8,

            FlagM = 16,

            FlagK = 32
        }
    }
}