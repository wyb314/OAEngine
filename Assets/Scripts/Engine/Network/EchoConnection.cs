using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Network
{
    class EchoConnection
    {
        protected struct ReceivedPacket
        {
            public int FromClient;
            public byte[] Data;
        }
    }
}
