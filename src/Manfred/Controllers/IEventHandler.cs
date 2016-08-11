using System;
using System.Threading.Tasks;
using Manfred.Models;

namespace Manfred.Controllers
{
    public interface IEventHandler
    {
        Task HandleEvent(EventLog e);
    }
}