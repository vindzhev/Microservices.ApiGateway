namespace Portal.Gateway
{
    using System;
    
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Builder;

    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    using Microsoft.IdentityModel.Logging;
    using IdentityServer4.AccessTokenValidation;

    using Ocelot.Middleware;
    using Ocelot.Provider.Consul;
    using Ocelot.Cache.CacheManager;
    using Ocelot.DependencyInjection;

    public class Program
    {
        public static void Main(string[] args) => BuildWebHost(args).Run();

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config
                        .SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
                        .AddJsonFile("appsettings.json", true, true)
                        .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true, true)
                        .AddJsonFile("ocelot.json", false, false)
                        .AddEnvironmentVariables();
                })
                .ConfigureServices(services =>
                {
                    services.AddCors();

                    Action<IdentityServerAuthenticationOptions> options = o =>
                    {
                        o.Authority = "https://localhost:5000";
                        o.SupportedTokens = SupportedTokens.Both;
                    };

                    services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                        .AddIdentityServerAuthentication("PortalGatewayAuthKey", options);

                    services.AddOcelot()
                        .AddConsul().AddCacheManager(x => x.WithDictionaryHandle());
                })
                .Configure(app =>
                {
                    IdentityModelEventSource.ShowPII = true;
                    app.UseCors(x => 
                        x.WithOrigins(new string[] { "http://localhost:4200" })
                        .AllowAnyHeader().AllowAnyMethod());

                    app.UseAuthentication();

                    app.UseOcelot().Wait();
                })
                .Build();
    }
}
