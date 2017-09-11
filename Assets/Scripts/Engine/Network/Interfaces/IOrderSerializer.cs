using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Engine.Network.Interfaces
{
    public interface IOrderSerializer
    {
        IOrder Deserialize(IWorld world, BinaryReader r);
        
        List<IOrder> Deserialize(IWorld world, byte[] bytes);

        byte[] Serialize(IOrder order);
    }
}
