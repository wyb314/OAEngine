﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

            //Settings.Name = OpenRA.Settings.SanitizedServerName(Settings.Name);

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

                DispatchOrdersToClient(newConn, 0, 0, new ServerOrderDefault("HandshakeRequest", request.Serialize()).Serialize());
            }
            catch (Exception e)
            {
                DropClient(newConn);
                //Log.Write("server", "Dropping client {0} because handshake failed: {1}", newConn.PlayerIndex.ToString(CultureInfo.InvariantCulture), e);
            }
        }

        int nextPlayerIndex;
        public int ChooseFreePlayerIndex()
        {
            return nextPlayerIndex++;
        }

        void DispatchOrdersToClient(IServerConnectoin<ClientDefault, ClientPingDefault> c, int client, int frame, byte[] data)
        {
            Console.WriteLine("DispatchOrdersToClient frame->{0}".F(frame));
            try
            {
                SendData(c.Socket, BitConverter.GetBytes(data.Length + 4));
                SendData(c.Socket, BitConverter.GetBytes(client));
                SendData(c.Socket, BitConverter.GetBytes(frame));
                SendData(c.Socket, data);
            }
            catch (Exception e)
            {
                DropClient(c);
                //Log.Write("server", "Dropping client {0} because dispatching orders failed: {1}",
                //    client.ToString(CultureInfo.InvariantCulture), e);
            }
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
            if (!PreConns.Remove(toDrop))
            {
                Conns.Remove(toDrop);

                var dropClient = LobbyInfo.Clients.FirstOrDefault(c1 => c1.Index == toDrop.PlayerIndex);
                if (dropClient == null)
                    return;

                var suffix = "";
                if (State == ServerState.GameStarted)
                    suffix = dropClient.IsObserver ? " (Spectator)" :  "";
                SendMessage("{0}{1} has disconnected.".F(dropClient.Name, suffix));

                // Send disconnected order, even if still in the lobby
                DispatchOrdersToClients(toDrop, 0, new ServerOrderDefault("Disconnected", "").Serialize());

                LobbyInfo.Clients.RemoveAll(c => c.Index == toDrop.PlayerIndex);
                LobbyInfo.ClientPings.RemoveAll(p => p.Index == toDrop.PlayerIndex);

                // Client was the server admin
                // TODO: Reassign admin for game in progress via an order
                if (Dedicated && dropClient.IsAdmin && State == ServerState.WaitingPlayers)
                {
                    // Remove any bots controlled by the admin
                    LobbyInfo.Clients.RemoveAll(c => c.Bot != null && c.BotControllerClientIndex == toDrop.PlayerIndex);

                    var nextAdmin = LobbyInfo.Clients.Where(c1 => c1.Bot == null)
                        .MinByOrDefault(c => c.Index);

                    if (nextAdmin != null)
                    {
                        nextAdmin.IsAdmin = true;
                        SendMessage("{0} is now the admin.".F(nextAdmin.Name));
                    }
                }

                DispatchOrders(toDrop, toDrop.MostRecentFrame, new byte[] { 0xbf });

                // All clients have left: clean up
                if (!Conns.Any())
                    foreach (var t in serverTraits.WithInterface<INotifyServerEmpty<ClientDefault,ClientPingDefault>>())
                        t.ServerEmpty(this);

                if (Conns.Any() || Dedicated)
                    SyncLobbyClients();

                if (!Dedicated && dropClient.IsAdmin)
                    Shutdown();
            }

            try
            {
                toDrop.Socket.Disconnect(false);
            }
            catch { }
        }

        public void SendMessage(string text)
        {
            DispatchOrdersToClients(null, 0, new ServerOrderDefault("Message", text).Serialize());

            if (Dedicated)
                Console.WriteLine("[{0}] {1}".F(DateTime.Now.ToString(Settings.TimestampFormat), text));
        }

        public void SyncLobbyClients()
        {
            if (State != ServerState.WaitingPlayers)
                return;

            // TODO: Only need to sync the specific client that has changed to avoid conflicts!
            //var clientData = LobbyInfo.Clients.Select(client => client.Serialize()).ToList();

            DispatchOrders(null, 0, new ServerOrderDefault("SyncLobbyClients", LobbyInfo.Clients.WriteToString()).Serialize());

            foreach (var t in serverTraits.WithInterface<INotifySyncLobbyInfo<ClientDefault, ClientPingDefault>>())
                t.LobbyInfoSynced(this);
        }

        public void DispatchOrders(IServerConnectoin<ClientDefault,ClientPingDefault> conn, int frame, byte[] data)
        {
            if (frame == 0 && conn != null)
            {
                //string log = "DispatchOrders InterpretServerOrders PlayerIndex->{0} frame->{1}".F(conn.PlayerIndex, frame);
                //Log.Write("wybserver", log);
                //Console.WriteLine(log);
                InterpretServerOrders(conn, data);
            }
            else
            {
                //string log = "DispatchOrders DispatchOrdersToClients PlayerIndex->{0} frame->{1}".F(conn != null ? conn.PlayerIndex : -1, frame);
                //Log.Write("wybserver", log);
                //Console.WriteLine(log);
                DispatchOrdersToClients(conn, frame, data);
            }

        }

        void InterpretServerOrders(IServerConnectoin<ClientDefault, ClientPingDefault> conn, byte[] data)
        {

            var ms = new MemoryStream(data);
            var br = new BinaryReader(ms);

            try
            {
                while (ms.Position < ms.Length)
                {
                    var so = ServerOrderDefault.Deserialize(br);
                    if (so == null) return;
                    InterpretServerOrder(conn, so);
                }
            }
            catch (EndOfStreamException) { }
            catch (NotImplementedException) { }
        }

        /// <summary>
        /// 解析特殊消息
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="so"></param>
        public void InterpretServerOrder(IServerConnectoin<ClientDefault, ClientPingDefault> conn, IServerOrder so)
        {
        }

        //IServerConnectoin<ClientDefault, ClientPingDefault>
        public void DispatchOrdersToClients(IServerConnectoin<ClientDefault,ClientPingDefault> conn, int frame, byte[] data)
        {
            var from = conn != null ? conn.PlayerIndex : 0;
            foreach (var c in Conns.Except(conn).ToList())
            {
                DispatchOrdersToClient(c, from, frame, data);
            }
                
        }

        public void EndGame()
        {
            foreach (var t in serverTraits.WithInterface<IEndGame<ClientDefault,ClientPingDefault>>())
                t.GameEnded(this);
        }

        public ClientDefault GetClient(IServerConnectoin<ClientDefault, ClientPingDefault> conn)
        {
            return LobbyInfo.ClientWithIndex(conn.PlayerIndex);
        }

        public void Shutdown()
        {
            State = ServerState.ShuttingDown;
        }

        public void StartGame()
        {
            listener.Stop();

            Console.WriteLine("[{0}] Game started", DateTime.Now.ToString(Settings.TimestampFormat));

            // Drop any unvalidated clients
            foreach (var c in PreConns.ToArray())
                DropClient(c);

            // Drop any players who are not ready
            foreach (var c in Conns.Where(c => GetClient(c).IsInvalid).ToArray())
            {
                SendOrderTo(c, "ServerError", "You have been kicked from the server!");
                DropClient(c);
            }

            // HACK: Turn down the latency if there is only one real player
            if (LobbyInfo.NonBotClients.Count() == 1)
                LobbyInfo.GlobalSettings.OrderLatency = 1;

            SyncLobbyInfo();
            State = ServerState.GameStarted;

            foreach (var c in Conns)
                foreach (var d in Conns)
                    DispatchOrdersToClient(c, d.PlayerIndex, 0x7FFFFFFF, new byte[] { 0xBF });

            DispatchOrders(null, 0,
                new ServerOrderDefault("StartGame", "").Serialize());

            foreach (var t in serverTraits.WithInterface<IStartGame<ClientDefault,ClientPingDefault>>())
                t.GameStarted(this);
        }

        public void SendOrderTo(IServerConnectoin<ClientDefault,ClientPingDefault> conn, string order, string data)
        {
            DispatchOrdersToClient(conn, 0, 0, new ServerOrderDefault(order, data).Serialize());
        }

        public void SyncLobbyInfo()
        {
            if (State == ServerState.WaitingPlayers) // Don't do this while the game is running, it breaks things!
                DispatchOrders(null, 0, new ServerOrderDefault("SyncInfo", LobbyInfo.Serialize()).Serialize());

            foreach (var t in serverTraits.WithInterface<INotifySyncLobbyInfo<ClientDefault, ClientPingDefault>>())
                t.LobbyInfoSynced(this);
        }
    }
}
