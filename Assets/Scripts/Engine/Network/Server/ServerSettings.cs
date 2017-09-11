using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Engine.Network.Server
{
    public class ServerSettings
    {
        private string name = "OpenRA Game";
        public string Name
        {
            private set { name = value; }
            get { return name; }
        }

        private int listenPort = 1234;

        public int ListenPort
        {
            private set { listenPort = value; }
            get { return this.listenPort; }
        }

        private int externalPort = 1234;

        public int ExternalPort
        {
            private set { this.externalPort = value; }
            get { return this.externalPort; }
        }

        private bool advertiseOnline = true;

        public bool AdvertiseOnline
        {
            private set { this.advertiseOnline = value; }
            get { return this.advertiseOnline; }
        }

        private string password = "";

        public string Password
        {
            private set { this.password = value; }
            get { return this.password; }
        }


        private bool discoverNatDevices = false;

        public bool DiscoverNatDevices
        {
            private set { this.discoverNatDevices = value; }
            get { return this.discoverNatDevices; }
        }

        private bool allowPortForward = true;

        public bool AllowPortForward
        {
            private set { this.allowPortForward = value; }
            get { return this.allowPortForward; }
        }

        private int natDiscoveryTimeout = 1000;

        public int NatDiscoveryTimeout
        {
            private set { this.natDiscoveryTimeout = value; }
            get { return this.natDiscoveryTimeout; }
        }

        private string map = null;

        public string Map
        {
            private set { this.map = value; }
            get { return this.map; }
        }

        private string[] ban = null;

        public string[] Ban
        {
            private set { this.ban = value; }
            get { return this.ban; }
        }

        private bool enableSingleplayer = false;

        public bool EnableSingleplayer
        {
            private set { this.enableSingleplayer = value; }
            get { return this.enableSingleplayer; }
        }


        private bool queryMapRepository = true;

        public bool QueryMapRepository
        {
            private set { this.queryMapRepository = value; }
            get { return this.queryMapRepository; }
        }

        private string timestampFormat = "s";

        public string TimestampFormat
        {
            private set { this.timestampFormat = value; }
            get { return this.timestampFormat; }
        }

        public ServerSettings Clone()
        {
            return (ServerSettings)MemberwiseClone();
        }
    }
}
