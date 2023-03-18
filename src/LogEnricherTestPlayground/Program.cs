using NewRelic.LogEnrichers;
using NewRelic.LogEnrichers.Serilog;
using Serilog;
namespace LogEnricherTestPlayground
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Host.UseSerilog((ctx, lc) => lc
                .Enrich.FromLogContext()
                .Enrich.WithNewRelicLogsInContext()
                .MinimumLevel.Information()
                .WriteTo.File(
                    path: "/var/log/NetServiceLogfile.log",
                    formatter: new NewRelicFormatter()
                        .WithPropertyMapping("ThreadId", NewRelicLoggingProperty.ThreadId)
                        .WithPropertyMapping("ThreadName", NewRelicLoggingProperty.ThreadName))
                .WriteTo.Console());

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.UseSerilogRequestLogging();

            app.Run();
        }
    }
}