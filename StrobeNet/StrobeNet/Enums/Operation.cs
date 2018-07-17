using System;
using System.Collections.Generic;
using System.Text;

namespace StrobeNet.Enums
{
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
}
