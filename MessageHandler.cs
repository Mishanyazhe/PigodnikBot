using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Threading;
using System.Threading.Tasks;
using Pigodnik;
using Cake.Core.IO;
using Telegram.Bot.Types.ReplyMarkups;

public class MessageHandler
{
    private readonly CouchDbService _couchDbService;
    private readonly WeatherService _weatherService;
    private readonly YandexApi _yandexApi;

    public MessageHandler(CouchDbService couchDbService, WeatherService weatherService, YandexApi yandexApi)
    {
        _couchDbService = couchDbService;
        _weatherService = weatherService;
        _yandexApi = yandexApi;
    }

    public async Task HandleMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message)
        {
            var message = update.Message;

            if (message?.Text != null)
            {
                await HandleTextMessage(botClient, message, cancellationToken);
            }
            else if (message?.Photo != null && message.Photo.Length > 0)
            {
                await HandlePhotoMessage(botClient, message, cancellationToken);
            }
            else if (message?.Video != null)
            {
                await HandleVideoMessage(botClient, message, cancellationToken);
            }
            else if (message?.Sticker != null)
            {
                await HandleStickerMessage(botClient, message, cancellationToken);
            }
        }
    }

    private async Task HandleTextMessage(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        long chatId = message.Chat.Id;
        string userName = message.Chat.FirstName ?? "Неизвестный пользователь";
        string userMessage = message.Text!.Trim();

        IReplyMarkup? replyMarkup = null;
        string logEntry = $"в {DateTime.Now} получено сообщение от {userName}" +
            $"({chatId}): {userMessage}";
        Console.WriteLine(logEntry);
        await SaveMessage.SaveMessageToFile(logEntry);

        if (userMessage.ToLower() == "/start")
        {
            await botClient.SendMessage(chatId,
                "Привет! Напиши название города, чтобы узнать погоду.",
                cancellationToken: cancellationToken);
            return;
        }

        string weather = await _weatherService.GetWeatherAsync(userMessage);
        string yandexWeather = await _yandexApi.GetWeatherAsync(userMessage);

        if (!yandexWeather.Contains("Такого населённого пункта у нас нет :/", StringComparison.OrdinalIgnoreCase))
        {
            replyMarkup = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData($"{userMessage}", $"city:{userMessage}") }
                });
        }

        await botClient.SendMessage(chatId,
            $"<u><b>Weather:</b></u> {weather}\n<u><b>Yandex:</b></u> {yandexWeather}",
            parseMode: ParseMode.Html,
            replyMarkup: replyMarkup,
            cancellationToken: cancellationToken);

        await _couchDbService.SaveMessageAsync(chatId, "text", userMessage, null);
    }

    private async Task HandlePhotoMessage(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        long chatId = message.Chat.Id;
        Console.WriteLine($"Получено фото: {message.Photo[^1]} от {message.Chat.FirstName} ({message.Chat.Id}) ");

        await botClient.SendMessage(chatId,
            "Фото мне не нужны.",
            cancellationToken: cancellationToken);

        var largestPhoto = message.Photo[^1];
        var filePath = await DownloadFileAsync(botClient, largestPhoto.FileId);
        Console.WriteLine($"Фото загружено: {filePath}");

        await _couchDbService.SaveMessageAsync(chatId, "photo", null, filePath);
    }

    private async Task HandleVideoMessage(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        long chatId = message.Chat.Id;
        Console.WriteLine($"Получено видео: {message.Video.FileId} от {message.Chat.FirstName} ({message.Chat.Id}) ");

        await botClient.SendMessage(chatId,
            "Видео мне не нужны.",
            cancellationToken: cancellationToken);
                
        var filePath = await DownloadFileAsync(botClient, message.Video.FileId);
        Console.WriteLine($"Видео загружено: {filePath}");
        await _couchDbService.SaveMessageAsync(chatId, "video", null, filePath);
    }

    private async Task HandleStickerMessage(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        long chatId = message.Chat.Id;
        Console.WriteLine($"Получен стикер: {message.Sticker.FileId} от {message.Chat.FirstName} ({message.Chat.Id}) ");

        await botClient.SendSticker(chatId,
            "CAACAgIAAxkBAAEKdApnRlzXe0mSj-2Sr--V_5jD19VHbgACGgADJeuTH6o1_iV2aiQfNgQ",
            cancellationToken: cancellationToken);
    }

    private static async Task<string> DownloadFileAsync(ITelegramBotClient botClient, string fileId)
    {
        try
        {
            var file = await botClient.GetFile(fileId);

            if (file?.FilePath == null)
            {
                throw new InvalidOperationException("Файл не найден.");
            }

            string localPath = System.IO.Path.Combine("downloads", file.FilePath);

            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(localPath)!);

            using (var fileStream = new FileStream(localPath, FileMode.Create))
            {
                await botClient.DownloadFile(file.FilePath, fileStream);
            }

            return localPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при загрузке файла: {ex.Message}");

            return null!;
        }
    }
}
