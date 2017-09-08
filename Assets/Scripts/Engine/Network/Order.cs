

using System.IO;

namespace Engine.Network
{
    public sealed class Order
    {

        public readonly string OrderString;

        public static Order Deserialize(World world, BinaryReader r)
        {
            return null;
        }

        public byte[] Serialize()
        {
            return null;
        }

    }
}
