using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Engine.Network.Interfaces;

namespace Engine.Network.Defaults
{
    public class OrderSerializerDefault : IOrderSerializer
    {
        public List<IOrder> Deserialize(IWorld world, byte[] bytes)
        {
            return bytes.ToOrderList(world);
        }

        public IOrder Deserialize(IWorld world, BinaryReader r)
        {
            throw new NotImplementedException();
        }

        public byte[] Serialize(IOrder order)
        {
            throw new NotImplementedException();
        }
    }
}
