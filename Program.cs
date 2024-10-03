using CoreHtmlToImage;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

var botClient = new TelegramBotClient(Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN") ?? "");
var grinch_url = Environment.GetEnvironmentVariable("GRINCH_URL11134");
using var cts = new CancellationTokenSource();
InlineKeyboardMarkup keyboard = new(new[]
{
    new[]
    {
        InlineKeyboardButton.WithCallbackData("ФТЕБ12140д", "ФТЕБ12140д"),
        InlineKeyboardButton.WithCallbackData("БІКСБ22140д", "БІКСБ22140д")
    }
});
var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = { }
};

botClient.StartReceiving(
    HandleUpdatesAsync,
    HandleErrorAsync,
    receiverOptions,
    cancellationToken: cts.Token);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Почав слухати @{me.Username}");
Console.ReadLine();

cts.Cancel();

async Task HandleUpdatesAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
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

async Task HandleMessage(ITelegramBotClient botClient, Message message)
{
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
async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery)
{
    await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"Секунду, шукаю..."); ;
    if (callbackQuery.Data.StartsWith("ФТЕБ12140д"))
    {
        using (var ms = new MemoryStream(ImageFromURL(grinch_url + "1013")))
        {
            await botClient.SendDocumentAsync(callbackQuery.Message.Chat.Id, new InputFileStream(ms, "image.png"),
                callbackQuery.Message.MessageThreadId, null, "Ось що знайшов на найближчий тиждень для групи ФТЕБ-1-21-4.0д");
        }
        await SendInlineKeyboards(botClient, callbackQuery.Message);
        return;
    }
    if (callbackQuery.Data.StartsWith("БІКСБ22140д"))
    {
        using (var ms = new MemoryStream(ImageFromURL(grinch_url + "928")))
        {
            await botClient.SendDocumentAsync(callbackQuery.Message.Chat.Id, new InputFileStream(ms, "image.png"),
                callbackQuery.Message.MessageThreadId, null, "Ось що знайшов на найближчий тиждень для групи БІКСБ-2-21-4.0д");
        }
        await SendInlineKeyboards(botClient, callbackQuery.Message);
        return;
    }
    await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"Шось не те, краще напиши @sasha_fpv : {callbackQuery.Data}");
    return;
}

Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Помилка: \n{apiRequestException.ErrorCode}\n{apiRequestException.Message}",
        _ => exception.ToString()
    };
    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}
byte[] ImageFromURL(string url)
{
    var converter = new HtmlConverter();
    return converter.FromUrl(url);
}
async Task SendInlineKeyboards(ITelegramBotClient botClient, Message message)
{
    await botClient.SendTextMessageAsync(message.Chat.Id, "Вибери свою групу:", replyMarkup: keyboard);
}