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
        
        public bool AllowPortForward = true;
        
        public int NatDiscoveryTimeout = 1000;
        
        public string Map = null;
        
        public string[] Ban = { };
        
        public bool EnableSingleplayer = false;

        public bool QueryMapRepository = true;

        public string TimestampFormat = "s";

        public ServerSettings Clone()
        {
            return (ServerSettings)MemberwiseClone();
        }
    }
}
