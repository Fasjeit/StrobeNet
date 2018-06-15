using System;
using System.Collections.Generic;
using System.Linq;

namespace StrobeNet
{
    using System.Text;

    public class Strobe
    {
        enum Role
        {
            /// <summary>
            /// set if we send the first transport message
            /// </summary>
            iInitiator,

            /// <summary>
            /// set if we receive the first transport message
            /// </summary>
            iResponder,

            /// <summary>
            /// starting value
            /// </summary>
            iNone,
        }


        /// <summary>
        /// The size of the authentication tag used in AEAD functions
        /// </summary>
        private const int MacLen = 16;

        private int duplexRate; // 1600/8 - security/4

        private int StrobeR; // duplexRate - 2

        /// <summary>
        /// used to avoid padding during the first permutation
        /// </summary>
        private bool initialized;

        /// <summary>
        /// start of the current operation (0 := previous block)
        /// </summary>
        private byte posBegin;

        private Role I0;

        /// <summary>
        /// streaming API
        /// </summary>
        private Flag curFlags;

        // duplex construction (see sha3.go)
        private ulong[] a = new ulong[25];

        byte[] buf;

        byte[] storage;

        /// <summary>
        /// used for duplex
        /// </summary>
        private byte[] tempStateBuf;

        // KEY inserts a key into the state.
        // It also provides forward secrecy.
        void Key(byte[] key)
        {
            Operate(false, "KEY", key, 0, false);
        }

        // PRF provides a hash of length `output_len` of all previous operations
        // It can also be used to generate random numbers, it is forward secure.
        byte[] PRF(int outputLen)
        {
            return Operate(false, "PRF", new byte[] { }, outputLen, false);
        }

        // AD allows you to authenticate Additional Data
        // it should be followed by a Send_MAC or Recv_MAC in order to truly work
        void AD(bool meta, byte[] additionalData)
        {
            Operate(meta, "AD", additionalData, 0, false);
        }

        [Flags]
        enum Flag
        {
            flagI,

            flagA,

            flagC,

            flagT,

            flagM,

            flagK,
        }


        private readonly Dictionary<string, Flag> operationMap =
            new Dictionary<string, Flag>()
                {
                    { "AD", Flag.flagA },
                    { "KEY", Flag.flagA | Flag.flagC },
                    { "PRF", Flag.flagI | Flag.flagA | Flag.flagC },
                    { "send_CLR", Flag.flagA | Flag.flagT },
                    { "recv_CLR", Flag.flagI | Flag.flagA | Flag.flagT },
                    { "send_ENC", Flag.flagA | Flag.flagC | Flag.flagT },
                    { "recv_ENC", Flag.flagI | Flag.flagA | Flag.flagC | Flag.flagT },
                    { "send_MAC", Flag.flagC | Flag.flagT },
                    { "recv_MAC", Flag.flagI | Flag.flagC | Flag.flagT },
                    { "RATCHET", Flag.flagC },
                };

        /// <summary>
        /// Operate runs an operation (see OperationMap for a list of operations).
        /// For operations that only require a length, provide the length via the
        /// length argument with an empty slice []byte{}. For other operations provide
        /// a zero length.
        /// Result is always retrieved through the return value. For boolean results,
        /// check that the first index is 0 for true, 1 for false.
        /// </summary>
        public byte[] Operate(bool meta, string operation, byte[] dataConst, int length, bool more)
        {
            // operation is valid?
            if (!this.operationMap.TryGetValue(operation, out var flags))
            {
                throw new Exception($"Not a valid operation: [{operation}]");
            }

            // operation is meta?
            if (meta)
            {
                flags |= Flag.flagM;
            }

            // does the operation requires a length?
            byte[] data;

            var a = (Flag.flagI | Flag.flagT);

            if (((flags & (Flag.flagI | Flag.flagT)) != (Flag.flagI | Flag.flagT))
                && (((flags & (Flag.flagI | Flag.flagA)) != Flag.flagA)))
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

            var cAfter = (flags & (Flag.flagC | Flag.flagI | Flag.flagT)) == (Flag.flagC | Flag.flagT);
            var cBefore = ((flags & Flag.flagC) != 0) && (!cAfter);

            this.Duplex(data, cBefore, cAfter, false);

            if ((flags & (Flag.flagI | Flag.flagA)) == (Flag.flagI | Flag.flagA))
            {
                // Return data for the application
                return data;
            }

            else if ((flags & (Flag.flagI | Flag.flagT)) == Flag.flagT)
            {
                // Return data for the transport.
                return data;
            }
            else if ((flags & (Flag.flagI | Flag.flagA | Flag.flagT)) == (Flag.flagI | Flag.flagT))
            {
                // Check MAC: all output bytes must be 0
                if (more)
                {
                    throw new Exception("not supposed to check a MAC with the 'more' streaming option");

                }
                byte failures = 0;
                foreach(var dataByte in data)
                {
                    failures |= dataByte;
                }
                return new byte[] { failures }; // 0 if correct, 1 if not
            }

            // Operation has no output
            return null;
        }

        // beginOp: starts an operation
        void BeginOp(Flag flags)
        {

            if ((flags & Flag.flagT) != 0)
            {
                if (this.I0 == Role.iNone)
                {
                    this.I0 = (Role)(flags & Flag.flagI);
                }
                flags ^= (Flag)(this.I0);
            }

            var oldBegin = this.posBegin;
            this.posBegin = (byte)(this.buf.Length + 1);
            var forceF = ((flags & (Flag.flagC | Flag.flagK)) != 0);

            this.Duplex(new byte[] { oldBegin, (byte)flags }, false, false, forceF);
        }


        void Duplex(Byte[] data, bool cbefore, bool cafter, bool forceF)
        {
            // process data block by block
            while (data.Length > 0)
            {
                var todo = this.StrobeR - this.buf.Length;
                if (todo > data.Length)
                {
                    todo = data.Length;
                }
                if (cbefore)
                {
                    outState(ref this.a, this.tempStateBuf);
                    for (var index = 0; index < todo; index ++)
                    {
                            data[index] ^= this.tempStateBuf[index + this.buf.Length];
                    }
                }

                this.buf = this.buf.Concat(data.Take(todo)).ToArray();

                if (cafter)
                {
                    outState(ref this.a, this.tempStateBuf);
                    for (var index = 0; index < todo; index++)
                    {
                        data[index] ^= this.tempStateBuf[index + this.buf.Length - todo];
                    }
                }

                // what's next for the loop?
                data = data.Skip(todo).ToArray();

                // If the duplex is full, time to XOR + padd + permutate.
                if (this.buf.Length == this.StrobeR)
                {
                    this.runF();
                }
            }
            // sometimes we the next operation to start on a new block
            if (forceF && this.buf.Length != 0)
            {
                this.runF();
            }
        }

        // InitStrobe allows you to initialize a new strobe instance with a customization string (that can be empty) and a security target (either 128 or 256).
        public Strobe(string customizationString, int security)
        {
            // compute security and rate
            if (security != 128 && security != 256)
            {
                throw new Exception("strobe: security must be set to either 128 or 256");
            }
            this.duplexRate = 1600 / 8 - security / 4;
            this.StrobeR = this.duplexRate - 2;
            // init vars
            this.storage = new byte[this.duplexRate];

            this.tempStateBuf = new byte[this.duplexRate];

            this.I0 = Role.iNone;
            this.initialized = false;
            // absorb domain + initialize + absorb custom string
            var domain = new byte[] { 1, (byte)(this.StrobeR + 2), 1, 0, 1, 12 * 8 };
            domain = domain.Concat(Encoding.UTF8.GetBytes("STROBEv1.0.2")).ToArray();
            this.buf = new byte[]{};
            this.Duplex(domain, false, false, true);

            this.initialized = true;
            this.Operate(true, "AD", Encoding.UTF8.GetBytes(customizationString), 0, false);
        }


        // runF: applies the STROBE's + cSHAKE's padding and the Keccak permutation
void runF()
        {
            if (this.initialized)
            {
                // if we're initialize we apply the strobe padding
                if (this.buf.Length > this.StrobeR)
                {
                    throw new Exception("strobe: buffer is never supposed to reach strobeR");

                }
                this.buf = this.buf.Concat(new byte[] {this.posBegin}).ToArray();
                this.buf = this.buf.Concat(new byte[] { 0x04 }).ToArray();
                var zerosStart = this.buf.Length;
                this.buf = this.storage.Take(this.duplexRate).ToArray();
        
                for (var i = zerosStart; i < this.duplexRate; i++)
                {
                    this.buf[i] = 0;

                }
                this.buf[this.duplexRate - 1] ^= 0x80;
                xorState(ref this.a, this.buf);
            }
            else if (this.buf.Length != 0)
            {
                // otherwise we just pad with 0s for xorState to work
                var zerosStart = buf.Length;
                this.buf = this.storage.Take(this.duplexRate).ToArray();
                for (var i = zerosStart; i < this.duplexRate; i++)
                {
                    this.buf[i] = 0;

                }
                xorState(ref this.a, this.buf);
            }

            // run the permutation
            Keccak.KeccakF1600(ref this.a, 24);
       
            // reset the buffer and set posBegin to 0
            // (meaning that the current operation started on a previous block)
            this.buf = new byte[] { };
            this.posBegin = 0;
        }

        //
        // Helper
        //

        // this only works for 8-byte alligned buffers
        void xorState(ref ulong[] state, byte[] buf)
        {
            var n = buf.Length / 8;
        
            for (var i = 0; i < n; i++)
            {
                //binary.LittleEndian.Uint64(buf);
                var a = BitConverter.ToUInt64(buf, 0);
                state[i] ^= a;
                buf = buf.Skip(8).ToArray();
            }
        }

        // this only works for 8-byte alligned buffers
        void outState(ref ulong[] state, byte[] b)
        {
            for (var i = 0; b.Length - i*8 > 0; i++)
            {
                var bytes = BitConverter.GetBytes(state[i]);
                Array.Copy(bytes, 0, b, 8*i, bytes.Length);
                //b = b.Skip(8).ToArray();
            }
        }




        // since the golang implementation does not absorb
        // things in the state "right away" (sometimes just
        // wait for the buffer to fill) we need a function
        // to properly print the state even when the state
        // is in this "temporary" state.
        public string debugPrintState() {
            // copy _storage into buf
            byte[] buf = new byte[1600 / 8];
            Array.Copy(this.storage, 0, buf, 0, this.buf.Length);
            // copy _state into state
            var state = new ulong[25];
            Array.Copy(this.a, 0, state, 0, this.a.Length);
            // xor
            xorState(ref state, buf);
            // print
            outState(ref state, buf);
            return BitConverter.ToString(buf).Replace("-", "");
        }
}
}
