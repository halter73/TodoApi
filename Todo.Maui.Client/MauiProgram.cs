using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace Todo.Maui.Client
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton<AuthenticatedClientProvider>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<LogInPage>();

            return builder.Build();
        }

    }
}