using System;
using System.Runtime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Manfred.Daos;
using Manfred.Models;
using HipChat.Net.Models.Response;

namespace Manfred.Controllers  {
    [Route("api/[controller]")]
    public class InstallationsController : Controller
    {
        public Settings Settings {get; set;}
        public IInstallationsRepository Installations {get; set;}
        public IOAuthRepository OAuth {get; set;}

        private ILogger logger;

        public InstallationsController(ILoggerFactory loggerFactory, IOptions<Settings> settings, IInstallationsRepository installationsRepo, IOAuthRepository oauthRepo)
        {
            logger = loggerFactory.CreateLogger<InstallationsController>();
            Settings = settings.Value;
            Installations = installationsRepo;
            OAuth = oauthRepo;
        }

        [HttpGet("{groupId}")]
        public async Task<IActionResult> Show(string groupId)
        {
            logger.LogInformation($"show GroupId={groupId}");

            var installation = await Installations.GetInstallationAsync(groupId);

            return Ok(installation);
        }

        [HttpGet("{groupId}/room/{roomId}")]
        public async Task<IActionResult> Show(string groupId, string roomId)
        {
            logger.LogInformation($"show GroupId={groupId} RoomId={roomId}");

            var installation = await Installations.GetInstallationAsync(groupId, roomId);

            return Ok(installation);
        }
        
        [HttpPost]
        public async Task<IActionResult> Installed([FromBody] Installed installed)
        {
            logger.LogInformation($"Installed GroupId={installed.GroupId} RoomId={installed.RoomId}");

            await Installations.CreateInstallationAsync(installed);

            return Ok();
        }

        [HttpGet("descriptor")]
        public Descriptor Descriptor()
        {
            return new Descriptor {
                Name = "Manfred",
                Key = "",
                Description = "Manfred HipChat Bot",
                Links = new Links {
                    Self = $"{Settings.Url}"
                },
                Capabilities = new Capabilities {
                    Installable = new Installable {
                        AllowGlobal = true,
                        AllowRoom = true,
                        CallbackUrl = $"{Settings.Url}/api/installations",
                        UpdateCallbackUrl = $"{Settings.Url}/api/installations/updated"
                    },
                    Configurable = new Configurable {
                        Url = $"{Settings.Url}/configuration"
                    }
                }
            };
        }

        [HttpDelete("{oauthId}")]
        public async Task<IActionResult> Uninstall(string oauthId)
        {
            logger.LogInformation($"uninstall OauthId={oauthId}");

            var oauth = await OAuth.GetOauthAsync(oauthId);

            logger.LogInformation($"removing installation GroupId={oauth.GroupId} RoomId={oauth.RoomId}");

            await Installations.RemoveInstallationAsync(oauth.GroupId, oauth.RoomId);

            logger.LogInformation($"removing oauth OauthId={oauthId}");

            await OAuth.RemoveOauthAsync(oauthId);

            return Ok();
        }
    }
}