using System;
using System.Collections.Generic;
using System.Text;

namespace StrobeNet.Enums
{
    /// <summary>
    /// Srobe operation
    /// </summary>
    public enum Operation
    {
        /// <summary>
        /// AD operation
        /// </summary>
        Ad,

        /// <summary>
        /// Key operation
        /// </summary>
        Key,

        /// <summary>
        /// PRF operation
        /// </summary>
        Prf,

        /// <summary>
        /// Send cleartext operation
        /// </summary>
        SendClr,

        /// <summary>
        /// Receive cleartext operation
        /// </summary>
        RecvClr,

        /// <summary>
        /// Send encrypted operation
        /// </summary>
        SendEnc,

        /// <summary>
        /// Receive encrepted operation
        /// </summary>
        RecvEnc,

        /// <summary>
        /// Send MAC operation
        /// </summary>
        SendMac,

        /// <summary> 
        /// Receive MAC operation
        /// </summary>
        RecvMac,

        /// <summary>
        /// Ratchet (rekey) operation
        /// </summary>
        Ratchet,
    }
}
