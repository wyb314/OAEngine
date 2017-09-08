using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Engine.Network
{
    public static class OrderIO
    {
        public static List<Order> ToOrderList(this byte[] bytes, World world)
        {
            var ms = new MemoryStream(bytes, 4, bytes.Length - 4);
            var reader = new BinaryReader(ms);
            var ret = new List<Order>();
            while (ms.Position < ms.Length)
            {
                var o = Order.Deserialize(world, reader);
                if (o != null)
                    ret.Add(o);
            }

            return ret;
        }
    }
}
