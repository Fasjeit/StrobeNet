using System;
using System.Collections.Generic;

namespace StrobeNet
{
    public class Strobe
    {
        /// <summary>
        /// The size of the authentication tag used in AEAD functions
        /// </summary>
        const int MacLen = 16;


        void Key(byte[] key)
        {

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


        private readonly Dictionary<string, Flag> _operationMap = new Dictionary<string, Flag>()
        {
            { "AD",       Flag.flagA},

            { "KEY",      Flag.flagA | Flag.flagC},

            { "PRF",      Flag.flagI | Flag.flagA | Flag.flagC},

            { "send_CLR", Flag.flagA | Flag.flagT},

            { "recv_CLR", Flag.flagI | Flag.flagA | Flag.flagT},

            { "send_ENC", Flag.flagA | Flag.flagC | Flag.flagT},

            { "recv_ENC", Flag.flagI | Flag.flagA | Flag.flagC | Flag.flagT},

            { "send_MAC", Flag.flagC | Flag.flagT},

            { "recv_MAC", Flag.flagI | Flag.flagC | Flag.flagT},

            { "RATCHET", Flag.flagC},
        };

        /// <summary>
        /// Operate runs an operation (see OperationMap for a list of operations).
        /// For operations that only require a length, provide the length via the
        /// length argument with an empty slice []byte{}. For other operations provide
        /// a zero length.
        /// Result is always retrieved through the return value. For boolean results,
        /// check that the first index is 0 for true, 1 for false.
        /// </summary>
        byte[] Operate(bool meta, string operation, byte[] dataConst, int length, bool more)
        {
            // operation is valid?
            if (!_operationMap.TryGetValue(operation, out var flags))
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

            if (
                ((flags & (Flag.flagI | Flag.flagT)) != (Flag.flagI | Flag.flagT)) &&
                (((flags & (Flag.flagI | Flag.flagA)) != Flag.flagA)))
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
                if (flags != _curFlags)
                {
                    throw new Exception("Flag should be the same when streaming operations");
                }
            }
            else{
                BeginOp(flags);
                _curFlags = flags;
            }

            // Operation
        }
    }
