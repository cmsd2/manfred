using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Manfred.Daos;
using HipChat.Net;
using HipChat.Net.Clients;
using HipChat.Net.Http;

namespace Manfred
{
    
    public class Startup
    {
        public IConfiguration Configuration {get; set;}

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets();
            }

            builder.AddEnvironmentVariables();

            Configuration = builder.Build();
        }
        
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddSingleton<IConfiguration>(Configuration);

            services.AddOptions();

            services.Configure<Settings>(Configuration.GetSection("Manfred"));
            
            services.AddSingleton<IEventLogsRepository>(sp => new EventLogsRepository(sp.GetService<ILoggerFactory>(), sp.GetService<IOptions<Settings>>()));
            services.AddSingleton<IMembershipRepository>(sp => new DynamoMembershipRepository(sp.GetService<ILoggerFactory>(), sp.GetService<IOptions<Settings>>()));
            services.AddSingleton<IWebHookRepository>(sp => new WebHooksRepository(sp.GetService<ILoggerFactory>(), sp.GetService<IOptions<Settings>>()));
            services.AddSingleton<IInstallationsRepository>(sp => new InstallationsRepository(sp.GetService<ILoggerFactory>(), sp.GetService<IOptions<Settings>>()));
            services.AddSingleton<ITokensRepository>(sp => new TokensRepository(sp.GetService<ILoggerFactory>(), sp.GetService<IOptions<Settings>>(), sp.GetService<IInstallationsRepository>()));

            var apiKey = Configuration.GetSection("Manfred").GetValue<string>("ApiKey");
            var hipChat = new HipChatClient(new ApiConnection(new Credentials(apiKey)));

            services.AddSingleton<HipChatClient>(hipChat);
            services.AddSingleton<IRoomsClient>(hipChat.Rooms);
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.UseMvc();

            loggerFactory.AddConsole();
        }
        
    }

}