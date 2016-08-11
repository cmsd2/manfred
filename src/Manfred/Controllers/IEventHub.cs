using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Manfred.Models;

namespace Manfred.Controllers
{
    public interface ISubscription {}
    
    public interface IEventHub
    {
        Task PublishEvent(EventLog e);
     
        ISubscription Subscribe(IEventHandler h);
        
        void Unsubscribe(ISubscription s);
    }
}