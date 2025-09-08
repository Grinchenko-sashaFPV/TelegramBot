using CoreHtmlToImage;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ScheduleTelegramBot.Services
{
    public class TelegramBotHostedService : BackgroundService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly SemaphoreSlim _logSemaphore = new(1, 1);
        private const string grinch_url = "https://dekanat.kubg.edu.ua/cgi-bin/timetable.cgi?n=700&group=";

        private readonly InlineKeyboardMarkup _keyboard = new(
        [
            [
                InlineKeyboardButton.WithCallbackData("БІКСм12514д", "БІКСм12514д"),
                InlineKeyboardButton.WithCallbackData("ФТЕБ", "ФТЕБ")
            ]
        ]);

        public TelegramBotHostedService(ITelegramBotClient botClient)
        {
            _botClient = botClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var me = await _botClient.GetMeAsync(stoppingToken);
            Console.WriteLine($"Почав слухати @{me.Username}");

            _botClient.StartReceiving(
                HandleUpdatesAsync,
                HandleErrorAsync,
                new ReceiverOptions { AllowedUpdates = { } },
                cancellationToken: stoppingToken);

            await Task.Delay(-1, stoppingToken);
        }

        private async Task HandleUpdatesAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update?.Message?.Text != null)
            {
                await HandleMessage(botClient, update.Message);
                return;
            }

            if (update.Type == UpdateType.CallbackQuery)
            {
                await HandleCallbackQuery(botClient, update.CallbackQuery);
                return;
            }
        }

        private async Task HandleMessage(ITelegramBotClient botClient, Message message)
        {
            await LogUsageAsync("Message", message.From, message.Chat.Id, message.Text ?? "-");

            if (message.Text == "/start")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Давай почнемо: /schedule");
                return;
            }

            if (message.Text == "/schedule")
            {
                await SendInlineKeyboards(botClient, message);
                return;
            }

            await botClient.SendTextMessageAsync(message.Chat.Id, $"Ти написав: \n{message.Text}");
        }

        private async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            var chatId = callbackQuery.Message?.Chat.Id ?? 0;
            await LogUsageAsync("Callback", callbackQuery.From, chatId, callbackQuery.Data ?? "-");

            await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"Секунду, шукаю...");

            if (callbackQuery.Data.StartsWith("ФТЕБ"))
            {
                using var ms = new MemoryStream(ImageFromURL(grinch_url + "-1375"));
                await botClient.SendDocumentAsync(callbackQuery.Message.Chat.Id,
                    new InputFileStream(ms, "image.png"),
                    caption: "Ось що знайшов на найближчий тиждень для групи ФТЕБ");
                await SendInlineKeyboards(botClient, callbackQuery.Message);
                return;
            }

            if (callbackQuery.Data.StartsWith("БІКСм12514д"))
            {
                using var ms = new MemoryStream(ImageFromURL(grinch_url + "-1280"));
                await botClient.SendDocumentAsync(callbackQuery.Message.Chat.Id,
                    new InputFileStream(ms, "image.png"),
                    caption: "Ось що знайшов на найближчий тиждень для групи БІКСм-1-25-14д");
                await SendInlineKeyboards(botClient, callbackQuery.Message);
                return;
            }

            await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id,
                $"Шось не те, краще напиши @sasha_fpv : {callbackQuery.Data}");
        }

        private Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Помилка: \n{apiRequestException.ErrorCode}\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);
            _ = LogErrorAsync(errorMessage);

            return Task.CompletedTask;
        }

        private byte[] ImageFromURL(string url)
        {
            var converter = new HtmlConverter();
            return converter.FromUrl(url);
        }

        private async Task SendInlineKeyboards(ITelegramBotClient botClient, Message message)
        {
            await botClient.SendTextMessageAsync(message.Chat.Id, "Вибери свою групу:", replyMarkup: _keyboard);
        }

        private async Task LogUsageAsync(string action, User? user, long chatId, string details)
        {
            var ts = DateTime.Now.ToString("o");
            var uid = user?.Id.ToString() ?? "null";
            var uname = string.IsNullOrEmpty(user?.Username) ? "-" : user!.Username;
            var fname = string.IsNullOrEmpty(user?.FirstName) ? "-" : user!.FirstName;
            var lname = string.IsNullOrEmpty(user?.LastName) ? "-" : user!.LastName;
            var safeDetails = details?.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ") ?? "-";

            var line = $"{ts}\t{action}\tUser:{uid}\t@{uname}\t{fname} {lname}\tChat:{chatId}\t{safeDetails}";

            try
            {
                await _logSemaphore.WaitAsync();
                await System.IO.File.AppendAllTextAsync("log.txt", line + Environment.NewLine);
            }
            finally
            {
                _logSemaphore.Release();
            }
        }

        private async Task LogErrorAsync(string errorMessage)
        {
            var ts = DateTime.Now.ToString("o");
            var line = $"{ts}\tERROR\t{errorMessage.Replace('\t', ' ').Replace('\n', ' ')}";

            try
            {
                await _logSemaphore.WaitAsync();
                await System.IO.File.AppendAllTextAsync("log.txt", line + Environment.NewLine);
            }
            finally
            {
                _logSemaphore.Release();
            }
        }
    }

}
