using System;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server
{
    public class BioRandWebServer2 : IAsyncDisposable
    {
        private readonly WebApplication _app;

        private BioRandWebServer2(WebApplication app)
        {
            _app = app;
        }

        public static Task<BioRandWebServer2> Create()
        {
            var builder = WebApplication.CreateBuilder();

            // Add services to the container.
            var config = Re4rConfiguration.GetDefault();
            builder.Services.AddSingleton<Re4rConfiguration>(config);
            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<EmailService>();
            builder.Services.AddSingleton<TwitchService>();
            builder.Services.AddSingleton<RandomizerService>();
            builder.Services.AddSingleton<UrlService>();
            builder.Services.AddSingleton<UserService>();

            builder.Services.AddSingleton<AuthService>();

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddControllers().AddApplicationPart(typeof(BioRandWebServer2).Assembly);
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            // if (_app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();

            app.MapControllers();

            app.Map("/", context =>
            {
                context.Response.Redirect("/swagger");
                return Task.CompletedTask;
            });

            return Task.FromResult(new BioRandWebServer2(app));
        }

        public ValueTask DisposeAsync()
        {
            return _app.DisposeAsync();
        }

        public Task RunAsync(string url)
        {
            return _app.RunAsync(url);
        }
    }
}
