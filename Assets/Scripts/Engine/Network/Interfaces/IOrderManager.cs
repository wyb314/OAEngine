using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Playables;

namespace Engine.Network.Interfaces
{
    public interface IOrderManager : IDisposable
    {
        int NetFrameNumber { get; }

        int LocalFrameNumber { get; }

        int FramesAhead { get; }
        
        IConnection Connection { get; }

        List<IOrder> localOrders { get; }

        IFrameData frameData { get; }

        IOrderSerializer orderSerializer { get; }

        IOrderProcessor orderProcessor { get; }

        ISyncReport syncReport { get; }

        IWorld World { get; }

        bool IsReadyForNextFrame { get; }
        
        void StartGame();


        void IssueOrders(IOrder[] orders);


        void IssueOrder(IOrder order);

        void TickImmediate();
        
        void Tick();
    }
}
