//namespace StrobeNet
//{
//    using System;
//    using System.Collections.Generic;
//    using System.Linq;
//    using System.Text;

//    public class StrobePy
//    {
//        private uint pos;
//        private Role I0;
//        private int R; // 1600/8 - security/4
//        /// <summary>
//        /// used to avoid padding during the first permutation
//        /// </summary>
//        private bool initialized;

//        private byte posbegin;

//        byte[] st; //storage



//        private readonly Dictionary<string, Flag> operationMap =
//            new Dictionary<string, Flag>()
//                {
//                    { "AD", Flag.flagA },
//                    { "KEY", Flag.flagA | Flag.flagC },
//                    { "PRF", Flag.flagI | Flag.flagA | Flag.flagC },
//                    { "send_CLR", Flag.flagA | Flag.flagT },
//                    { "recv_CLR", Flag.flagI | Flag.flagA | Flag.flagT },
//                    { "send_ENC", Flag.flagA | Flag.flagC | Flag.flagT },
//                    { "recv_ENC", Flag.flagI | Flag.flagA | Flag.flagC | Flag.flagT },
//                    { "send_MAC", Flag.flagC | Flag.flagT },
//                    { "recv_MAC", Flag.flagI | Flag.flagC | Flag.flagT },
//                    { "RATCHET", Flag.flagC },
//                };

//        /// <summary>
//        /// streaming API
//        /// </summary>
//        private Flag curFlags;

//        public StrobePy(byte[] proto, int security, object copyOf = null, bool doInit = false)
//        {
//            if (copyOf == null)
//            {
//                this.pos = this.posbegin;
//                this.R = 1600 / 8 - security / 4;

//                // Domain separation doesn't use Strobe padding
//                this.initialized = false;
//                this.st = new byte[1600 / 8];

//                var domain = new byte[] { 1, (byte)(this.R + 2), 1, 0, 1, 12 * 8 };
//                domain = domain.Concat(Encoding.UTF8.GetBytes("STROBEv1.0.2")).ToArray();

//                if (doInit)
//                {
//                    this.Duplex(domain, forceF: true);
//                }

//                // cSHAKE separation is done.
//                // Turn on Strobe padding and do per-proto separation
//                this.R -= 2;
//                this.initialized = true;
//                if (doInit)
//                {
//                    this.Operate(Flag.flagA | Flag.flagM, proto);
//                }
//            }
//            else
//            {
//                //toDo implement copy
//                throw new NotSupportedException();
//            }
//        }

//        public Byte[] Ad(
//            byte[] data, bool more = false, Flag metaFlags = Flag.flagA | Flag.flagM, byte[] metadata = null)
//        {
//            return this.Operate(this.operationMap["AD"], data, more, metaFlags, metadata);
//        }

//        public byte[] Key(byte[] data)
//        {
//            return this.Operate(this.operationMap["KEY"], data);
//        }

//        public byte[] Operate(
//            string operation,
//            byte[] data,
//            bool more = false,
//            Flag metaFlags = Flag.flagA | Flag.flagM,
//            byte[] metadata = null)
//        {
//            // operation is valid?
//            if (!this.operationMap.TryGetValue(operation, out var flags))
//            {
//                throw new Exception($"Not a valid operation: [{operation}]");
//            }

//            // operation is meta?
//            if (metadata != null)
//            {
//                flags |= Flag.flagM;
//            }
//            return this.Operate(flags, data, more, metaFlags, metadata);
//        }


//        public byte[] Operate(
//            Flag flags,
//            byte[] data,
//            bool more = false,
//            Flag metaFlags = Flag.flagA | Flag.flagM,
//            byte[] metadata = null)
//        {
//            byte[] metaOut = new byte[] { };
//            if (more)
//            {
//                if (flags != this.curFlags)
//                {
//                    //ex
//                }

//            }
//            else
//            {
//                if (metadata != null)
//                {
//                    metaOut = this.Operate(metaFlags, metadata);
//                }

//                this.BeginOp(flags);
//                this.curFlags = flags;
//            }

//            if (((flags & (Flag.flagI | Flag.flagT)) != (Flag.flagI | Flag.flagT))
//                && (((flags & (Flag.flagI | Flag.flagA)) != Flag.flagA)))
//            {
//                if (data.Length == 0)
//                {
//                    throw new Exception("A data with non-zero length should be set for this operation");
//                }
//            }

//            // The actual processing code is just duplex

//            var cAfter = (flags & (Flag.flagC | Flag.flagI | Flag.flagT)) == (Flag.flagC | Flag.flagT);
//            var cBefore = ((flags & Flag.flagC) != 0) && (!cAfter);
//            var processed = this.Duplex(data, cBefore, cAfter);

//            // Determine what to do with the output.
//            if ((flags & (Flag.flagI | Flag.flagA)) == (Flag.flagI | Flag.flagA))
//            {
//                // Return data to the application
//                return metaOut.Concat(processed).ToArray();
//            }

//            else if ((flags & (Flag.flagI | Flag.flagT)) == Flag.flagT)
//            {
//                // Return data to the transport.
//                // A fancier implementation might send it directly.
//                return metaOut.Concat(processed).ToArray();
//            }
//            else if ((flags & (Flag.flagI | Flag.flagA | Flag.flagT)) == (Flag.flagI | Flag.flagT))
//            {
//                //Check MAC
//                if (more)
//                {
//                    //#Q_
//                    throw new Exception();
//                }
//                byte failures = 0;
//                foreach (var databyte in processed)
//                {
//                    failures |= databyte;
//                }

//                if (failures != 0)
//                {
//                    //#Q_ MAC not correwnt exception. authenticated exeption
//                    throw new Exception();
//                }
//                return metaOut;
//            }
//            else
//            {
//                //Operation has no output data, but maybe output metadata
//                return metaOut;
//            }
//        }

//        private void BeginOp(Flag flags)
//        {
//            // Adjust direction information so that sender and receiver agree
//            if ((flags & Flag.flagT) != 0)
//            {

//                if (this.I0 == Role.iNone)
//                {
//                    this.I0 = (Role)(flags & Flag.flagT);
//                }

//                flags ^= (Flag)this.I0;
//            }

//            var oldBegin = this.posbegin;
//            this.posbegin = (byte)(this.pos + 1);

//            this.Duplex(new byte[] { oldBegin, (byte)flags }, forceF: (flags & (Flag.flagC | Flag.flagK)) != 0);
//        }

//        void RunF()
//        {
//            if (this.initialized)
//            {
//                this.st[this.pos] ^= this.posbegin;
//                this.st[this.pos + 1] ^= 0x04;
//                this.st[this.R + 1] ^= 0x80;
//            }
//            Keccak.KeccakF1600(ref this.st, 24);
//            this.pos = this.posbegin = 0;
//        }


//        private byte[] Duplex(byte[] data, bool cbefore = false, bool cafter = false, bool forceF = false)
//        {
//            if (cbefore && cafter)
//            {
//                throw new Exception($"either {nameof(cbefore)} or {nameof(cafter)} should be set to false");
//            }

//            // Copy data
//            var newData = (byte[])data.Clone();

//            for (int i = 0; i < newData.Length; i++)
//            {
//                if (cbefore)
//                {
//                    newData[i] ^= this.st[this.pos];
//                }

//                if (cafter)
//                {
//                    newData[i] = this.st[this.pos];
//                }

//                this.pos += 1;
//                if (this.pos == this.R)
//                {
//                    this.RunF();
//                }
//            }

//            if (forceF && this.pos != 0)
//            {
//                this.RunF();
//            }

//            return newData;
//        }

//        // since the golang implementation does not absorb
//        // things in the state "right away" (sometimes just
//        // wait for the buffer to fill) we need a function
//        // to properly print the state even when the state
//        // is in this "temporary" state.
//        public string DebugPrintState()
//        {
//            return BitConverter.ToString(this.st).Replace("-", "");
//        }

//    }

//    enum Role
//    {
//        /// <summary>
//        /// set if we send the first transport message
//        /// </summary>
//        iInitiator,

//        /// <summary>
//        /// set if we receive the first transport message
//        /// </summary>
//        iResponder,

//        /// <summary>
//        /// starting value
//        /// </summary>
//        iNone,
//    }

//    [Flags]
//    public enum Flag
//    {
//        flagI,

//        flagA,

//        flagC,

//        flagT,

//        flagM,

//        flagK,
//    }
//}
