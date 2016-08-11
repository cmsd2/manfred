using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Manfred.Models;

namespace Manfred.Controllers
{
    public class EventHub : IEventHub
    {
        private ILogger logger;
        
        private Settings Settings {get; set;}
        
        private Dictionary<ISubscription, IEventHandler> handlers = new Dictionary<ISubscription, IEventHandler>();
        
        public EventHub(ILoggerFactory loggerFactory, IOptions<Settings> settings)
        {
            logger = loggerFactory.CreateLogger<EventHub>();
            
            Settings = settings.Value;
        }
        
        public Task PublishEvent(EventLog e)
        {
            return Task.WhenAll(handlers.Select(h => h.Value.HandleEvent(e)));
        }
        
        public ISubscription Subscribe(IEventHandler h)
        {
            var sub = new Subscription();
            handlers.Add(sub, h);
            return sub;
        }
        
        public void Unsubscribe(ISubscription s)
        {
            handlers.Remove(s);
        }
    }
    
    public class Subscription : ISubscription
    {
        
    }
}