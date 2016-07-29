using System;
using System.Runtime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Manfred.Controllers  {
    public class HelloController
    {
        public readonly string Greeting;

        public HelloController(IOptions<Settings> settings)
        {
            Greeting = settings.Value.ApiKey;
        }
        
        [HttpGet("api/hello")]
        public object Hello()
        {
            return new
            {
                message = Greeting,
                time = DateTime.Now
            };
        }
    }
}