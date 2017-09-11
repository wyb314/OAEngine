using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Engine.Network.Enums;
using Engine.Network.Interfaces;
using Engine.Primitives;

namespace Engine.Network.Defaults
{
    public class ServerDefault : IServer<ClientDefault, ClientPingDefault>
    {
        public List<ClientDefault> Conns { private set; get; }

        public IPAddress Ip { private set; get; }

        public Session<ClientDefault, ClientPingDefault> LobbyInfo { private set; get; }

        public int Port { private set; get; }

        public List<ClientDefault> PreConns { private set; get; }
        
        public TypeDictionary serverTraits { private set;get; }
        
        public ServerState State { private set; get; }


        public ServerDefault(IPEndPoint endpoint, ServerSettings settings, ModData modData, bool dedicated)
        {
        }





        public void DropClient(IServerConnectoin toDrop)
        {
            throw new NotImplementedException();
        }

        public void EndGame()
        {
            throw new NotImplementedException();
        }

        public ClientDefault GetClient(IServerConnectoin conn)
        {
            throw new NotImplementedException();
        }

        public void Shutdown()
        {
            throw new NotImplementedException();
        }

        public void StartGame()
        {
            throw new NotImplementedException();
        }
    }
}
