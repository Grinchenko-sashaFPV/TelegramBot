using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ScheduleTelegramBot.Services;
using Telegram.Bot;

await Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
        if (string.IsNullOrEmpty(token))
            throw new InvalidOperationException("TELEGRAM_BOT_TOKEN is not set");

        services.AddSingleton<ITelegramBotClient>(sp => new TelegramBotClient(token));
        services.AddHostedService<TelegramBotHostedService>();
    })
    .Build()
    .RunAsync();
