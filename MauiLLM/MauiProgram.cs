using Microsoft.Extensions.Logging;
using MauiLLM.Services;
using MauiLLM.ViewModels;
using MauiLLM.Views;

namespace MauiLLM
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

            builder.Services.AddSingleton<LocalLlmService>();
            builder.Services.AddSingleton<ModelDownloadService>();
            builder.Services.AddSingleton<ChatViewModel>();
            builder.Services.AddSingleton<ChatPage>();
            builder.Services.AddSingleton<AppShell>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
