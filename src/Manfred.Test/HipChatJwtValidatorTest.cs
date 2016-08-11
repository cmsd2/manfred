using System;
using System.Threading.Tasks;
using System.Security.Claims;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Manfred.Models;
using Manfred.Daos;

namespace Manfred.Controllers
{
    public class HipChatJwtValidatorTest
    {
        private Installation installation;
        private IInstallationsRepository installations;
        private HipChatJwtValidator jwtValidator;

        public HipChatJwtValidatorTest()
        {
            ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole();
            installations = new InMemoryInstallationsRepository();
            installation = new Installation {
                GroupId = "123",
                RoomId = "1824420",
                OauthId = "866dd7e9-8e45-4e2f-888d-2c9d2ed5580b",
                OauthSecret = "4d7353b1-905e-4565-8acd-b66e37b71582"
            };
            installations.CreateInstallationAsync(installation);
            jwtValidator = new HipChatJwtValidator(loggerFactory, Options.Create<Settings>(new Settings()), installations);
        }

        [Fact]
        public async void ShouldValidateJwt()
        {
            var token = jwtValidator.SignToken(installation, new ClaimsIdentity());
            
            await jwtValidator.Validate(token);
        }

        [Fact]
        public async void ShouldThrowOnNullJwt()
        {
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await jwtValidator.Validate(null));
        }

        [Fact]
        public async void ShouldThrowOnBlankJwt()
        {
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await jwtValidator.Validate(" "));
        }

        [Fact]
        public async void ShouldThrowOnBadJwt()
        {
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await jwtValidator.Validate("JWT FOO"));
        }
    }
}