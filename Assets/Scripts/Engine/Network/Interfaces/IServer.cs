using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Engine.Primitives;
using Engine.Support;
using Engine.Network.Enums;

namespace Engine.Network.Interfaces
{
    public interface IServer<T, U> where T : IClient where U : IClientPing
    {
        IPAddress Ip { get; }

        int Port { get; }
        
        List<T> Conns { get; }

        List<T> PreConns { get; }
        
        Session<T, U> LobbyInfo { get; }

        TypeDictionary serverTraits { get; }

        ServerState State { get; }

        void StartGame();

        void EndGame();

        void DropClient(IServerConnectoin toDrop);

        void Shutdown();
        
        T GetClient(IServerConnectoin conn);


    }
}
