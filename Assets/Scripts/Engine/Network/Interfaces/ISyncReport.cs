﻿using System;
using System.Collections.Generic;
using UnityEngine.Playables;

namespace Engine.Network.Interfaces
{
    public interface ISyncReport
    {
        void DumpSyncReport(int frame, IEnumerable<ClientOrder> orders);

        void UpdateSyncReport();
    }
}
