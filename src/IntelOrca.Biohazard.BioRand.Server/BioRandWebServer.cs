using System;
using System.IO;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;
using Serilog;

namespace IntelOrca.Biohazard.BioRand.Server
{
    public class BioRandWebServer : IAsyncDisposable
    {
        private readonly WebApplication _app;

        private BioRandWebServer(WebApplication app)
        {
            _app = app;
        }

        public static Task<BioRandWebServer> Create()
        {
            var builder = WebApplication.CreateBuilder();
            ConfigureLogger(builder);

            // Add services to the container.
            var config = BioRandServerConfiguration.GetDefault();
            builder.Services.AddSingleton<BioRandServerConfiguration>(config);
            builder.Services.AddSingleton<BioRandService>();
            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<EmailService>();
            builder.Services.AddSingleton<TwitchService>();
            builder.Services.AddSingleton<RandomizerService>();
            builder.Services.AddSingleton<UrlService>();
            builder.Services.AddSingleton<UserService>();

            builder.Services.AddSingleton<AuthService>();

            builder.Services.AddHostedService<GenerationService>();

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddControllers().AddApplicationPart(typeof(BioRandWebServer).Assembly);

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseCors(policy =>
            {
                policy.AllowAnyOrigin();
                policy.AllowAnyHeader();
                policy.AllowAnyMethod();
            });

            // Configure the HTTP request pipeline.
            // if (_app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.MapControllers();

            app.Map("/", context =>
            {
                context.Response.Redirect("/swagger");
                return Task.CompletedTask;
            });

            app.Map("/favicon.ico", async context =>
            {
                context.Response.ContentType = MimeTypes.GetMimeType("favicon.ico");
                await context.Response.Body.WriteAsync(Resources.favicon);
            });

            return Task.FromResult(new BioRandWebServer(app));
        }

        public ValueTask DisposeAsync()
        {
            return _app.DisposeAsync();
        }

        public async Task RunAsync(string url)
        {
            var bioRandService = _app.Services.GetService<BioRandService>();
            if (bioRandService != null)
            {
                await bioRandService.Initialize();
            }
            await _app.RunAsync(url);
        }

        private static void ConfigureLogger(WebApplicationBuilder builder)
        {
            var logDirectory = BioRandServerConfiguration.GetLogDirectory();
            var logFile = Path.Combine(logDirectory, "api.biorand-re4r.log");

            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(logFile, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            builder.Host.UseSerilog();
        }
    }
}
