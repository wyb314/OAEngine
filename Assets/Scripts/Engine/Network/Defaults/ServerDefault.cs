using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Engine.Network.Enums;
using Engine.Network.Interfaces;
using Engine.Network.Server;
using Engine.Primitives;
using Engine.Support;
using System.Threading;

namespace Engine.Network.Defaults
{
    public class ServerDefault : IServer<ClientDefault, ClientPingDefault>
    {
        public List<IServerConnectoin<ClientDefault, ClientPingDefault>> Conns { private set; get; }

        public List<IServerConnectoin<ClientDefault, ClientPingDefault>> PreConns { private set; get; }

        public IPAddress Ip { private set; get; }

        public Session<ClientDefault, ClientPingDefault> LobbyInfo { private set; get; }

        public int Port { private set; get; }
        
        public TypeDictionary serverTraits { private set;get; }
        
        public ServerState State { private set; get; }

        public bool Dedicated { private set; get; }

        public ModData ModData { private set; get; }

        public ServerSettings Settings { private set; get; }
        
        readonly int randomSeed;
        public readonly MersenneTwister Random = new MersenneTwister();
        readonly TcpListener listener;

        public ServerDefault(IPEndPoint endpoint, ServerSettings settings, ModData modData, bool dedicated)
        {
            listener = new TcpListener(endpoint);
            listener.Start();
            var localEndpoint = (IPEndPoint)listener.LocalEndpoint;
            Ip = localEndpoint.Address;
            Port = localEndpoint.Port;
            Dedicated = dedicated;
            Settings = settings;

            Settings.Name = OpenRA.Settings.SanitizedServerName(Settings.Name);

            ModData = modData;

            randomSeed = (int)DateTime.Now.ToBinary();

            // UPnP is only supported for servers created by the game client.
            //if (!dedicated && Settings.AllowPortForward)
            //    UPnP.ForwardPort(Settings.ListenPort, Settings.ExternalPort).Wait();

            foreach (var trait in modData.Manifest.ServerTraits)
                serverTraits.Add(modData.ObjectCreator.CreateObject<ServerTrait>(trait));

            LobbyInfo = new Session<ClientDefault, ClientPingDefault>
            {
                GlobalSettings =
                {
                    RandomSeed = randomSeed,
                    Map = settings.Map,
                    ServerName = settings.Name,
                    EnableSingleplayer = settings.EnableSingleplayer || !dedicated,
                    GameUid = Guid.NewGuid().ToString()
                }
            };

            new Thread(_ =>
            {
                foreach (var t in serverTraits.WithInterface<INotifyServerStart<ClientDefault,ClientPingDefault>>())
                    t.ServerStarted(this);

                //Log.Write("server", "Initial mod: {0}", ModData.Manifest.Id);
                //Log.Write("server", "Initial map: {0}", LobbyInfo.GlobalSettings.Map);

                var timeout = serverTraits.WithInterface<ITick<ClientDefault, ClientPingDefault>>().Min(t => t.TickTimeout);
                for (;;)
                {
                    var checkRead = new List<Socket>();
                    if (State == ServerState.WaitingPlayers)
                        checkRead.Add(listener.Server);

                    checkRead.AddRange(Conns.Select(c => c.Socket));
                    checkRead.AddRange(PreConns.Select(c => c.Socket));

                    if (checkRead.Count > 0)
                        Socket.Select(checkRead, null, null, timeout);

                    if (State == ServerState.ShuttingDown)
                    {
                        EndGame();
                        break;
                    }

                    foreach (var s in checkRead)
                    {
                        if (s == listener.Server)
                        {
                            AcceptConnection();
                            continue;
                        }

                        var preConn = PreConns.SingleOrDefault(c => c.Socket == s);
                        if (preConn != null)
                        {
                            preConn.ReadData(this);
                            continue;
                        }

                        var conn = Conns.SingleOrDefault(c => c.Socket == s);
                        if (conn != null)
                            conn.ReadData(this);
                    }

                    foreach (var t in serverTraits.WithInterface<ITick<ClientDefault,ClientPingDefault>>())
                        t.Tick(this);

                    if (State == ServerState.ShuttingDown)
                    {
                        EndGame();
                        //if (!dedicated && Settings.AllowPortForward)
                        //    UPnP.RemovePortForward().Wait();
                        break;
                    }
                }

                foreach (var t in serverTraits.WithInterface<INotifyServerShutdown<ClientDefault, ClientPingDefault>>())
                    t.ServerShutdown(this);

                PreConns.Clear();
                Conns.Clear();
                try { listener.Stop(); }
                catch { }
            })
            { IsBackground = true }.Start();

        }


        void AcceptConnection()
        {
            Socket newSocket;

            try
            {
                if (!listener.Server.IsBound)
                    return;

                newSocket = listener.AcceptSocket();
            }
            catch (Exception e)
            {
                /* TODO: Could have an exception here when listener 'goes away' when calling AcceptConnection! */
                /* Alternative would be to use locking but the listener doesn't go away without a reason. */
                //Log.Write("server", "Accepting the connection failed.", e);
                return;
            }

            var newConn = new ServerConnectoinDefault(){ Socket = newSocket };
            try
            {
                newConn.Socket.Blocking = false;
                newConn.Socket.NoDelay = true;

                // assign the player number.
                newConn.PlayerIndex = ChooseFreePlayerIndex();
                SendData(newConn.Socket, BitConverter.GetBytes(ProtocolVersion.Version));
                SendData(newConn.Socket, BitConverter.GetBytes(newConn.PlayerIndex));
                PreConns.Add(newConn);

                // Dispatch a handshake order
                var request = new HandshakeRequest
                {
                    Mod = ModData.Manifest.Id,
                    Version = ModData.Manifest.Metadata.Version,
                    Map = LobbyInfo.GlobalSettings.Map
                };

                DispatchOrdersToClient(newConn, 0, 0, new ServerOrder("HandshakeRequest", request.Serialize()).Serialize());
            }
            catch (Exception e)
            {
                DropClient(newConn);
                Log.Write("server", "Dropping client {0} because handshake failed: {1}", newConn.PlayerIndex.ToString(CultureInfo.InvariantCulture), e);
            }
        }

        int nextPlayerIndex;
        public int ChooseFreePlayerIndex()
        {
            return nextPlayerIndex++;
        }

        static void SendData(Socket s, byte[] data)
        {
            var start = 0;
            var length = data.Length;

            // Non-blocking sends are free to send only part of the data
            while (start < length)
            {
                SocketError error;
                var sent = s.Send(data, start, length - start, SocketFlags.None, out error);
                if (error == SocketError.WouldBlock)
                {
                    //Log.Write("server", "Non-blocking send of {0} bytes failed. Falling back to blocking send.", length - start);
                    s.Blocking = true;
                    sent = s.Send(data, start, length - start, SocketFlags.None);
                    s.Blocking = false;
                }
                else if (error != SocketError.Success)
                    throw new SocketException((int)error);

                start += sent;
            }
        }

        public void DropClient(IServerConnectoin<ClientDefault, ClientPingDefault> toDrop)
        {
            throw new NotImplementedException();
        }

        public void EndGame()
        {
            throw new NotImplementedException();
        }

        public ClientDefault GetClient(IServerConnectoin<ClientDefault, ClientPingDefault> conn)
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
