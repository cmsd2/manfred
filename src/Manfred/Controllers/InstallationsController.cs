using System;
using System.Runtime;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Manfred.Daos;
using Manfred.Models;
using HipChat.Net.Models.Response;
using Manfred.ViewModels;

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

            if(installation != null)
            {
                return Ok(new InstallationView(installation));
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("{groupId}/room/{roomId}")]
        public async Task<IActionResult> Show(string groupId, string roomId)
        {
            logger.LogInformation($"show GroupId={groupId} RoomId={roomId}");

            var installation = await Installations.GetInstallationAsync(groupId, roomId);

            if(installation != null)
            {
                return Ok(new InstallationView(installation));
            }
            else
            {
                return NotFound();
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> Installed([FromBody] Installed installed)
        {
            logger.LogInformation($"Installed GroupId={installed.GroupId} RoomId={installed.RoomId}");

            await Installations.CreateInstallationAsync(new Installation {
                OauthId = installed.OauthId,
                GroupId = installed.GroupId,
                RoomId = installed.RoomId,
                CapabilitiesUrl = installed.CapabilitiesUrl
            });

            await OAuth.CreateOauthAsync(new Oauth {
                OauthId = installed.OauthId,
                OauthSecret = installed.OauthSecret,
                GroupId = installed.GroupId,
                RoomId = installed.RoomId,
                CapabilitiesUrl = installed.CapabilitiesUrl
            });

            return Ok();
        }

        [HttpGet("descriptor")]
        public Descriptor Descriptor()
        {
            return new Descriptor {
                Name = "Manfred",
                Key = "uk.org.octomonkey.cmsd2.manfred",
                Description = "Manfred HipChat Bot",
                Links = new Links {
                    Self = $"{Settings.Url}"
                },
                Capabilities = new Capabilities {
                    HipchatApiConsumer = new HipchatApiConsumer {
                        FromName = "Manfred",
                        Scopes = new List<string> {
                            "send_message",
                            "send_notification",
                            "view_group",
                            "view_messages",
                            "view_room"
                        },
                        Avatar = new Avatar {
                            Url = "https://www.gravatar.com/avatar/00000000000000000000000000000000?d=retro&s=64",
                            Url2x = "https://www.gravatar.com/avatar/00000000000000000000000000000000?d=retro&s=128"
                        }
                    },
                    Installable = new Installable {
                        AllowGlobal = true,
                        AllowRoom = true,
                        CallbackUrl = $"{Settings.Url}/api/installations",
                        UpdateCallbackUrl = $"{Settings.Url}/api/installations/updated"
                    },
                    Configurable = new Configurable {
                        Url = $"{Settings.Url}/configuration"
                    },
                    Webhook = new List<Webhook> {
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