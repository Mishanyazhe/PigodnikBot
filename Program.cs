using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Pigodnik;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using System.Reflection.Metadata;
using Quartz;
using Quartz.Impl;

class Program
{
    private static TelegramBotClient? botClient;

    //static async Task Main(string[] args)
    //{
    //    AppConfiguration = new ConfigurationBuilder()
    //        .SetBasePath(Directory.GetCurrentDirectory())
    //        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    //        .AddEnvironmentVariables()
    //        .Build();

    //    var botToken = AppConfiguration["BotSettings:BotToken"];

    //    botClient = new TelegramBotClient(botToken);

    //    using var cts = new CancellationTokenSource();
    //    var couchDbService = new CouchDbService(AppConfiguration);

    //    botClient.StartReceiving(
    //        HandleUpdateAsync,
    //        HandleErrorAsync,
    //        cancellationToken: cts.Token
    //    );

    //    await DistributionInitializing();

    //    Console.WriteLine("Бот запущен. Нажмите Enter для завершения.");
    //    Console.ReadLine();
    //    cts.Cancel();
    //}

    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var botToken = configuration["BotSettings:BotToken"];
        //var botToken = Environment.GetEnvironmentVariable("BotSettings__BotToken");
        botClient = new TelegramBotClient(botToken);

        var couchDbService = new CouchDbService(configuration);
        var weatherService = new WeatherService(configuration);
        var yandexApi = new YandexApi(configuration);

        var messageHandler = new MessageHandler(couchDbService, weatherService, yandexApi);
        var buttonHandler = new ButtonHandler(weatherService, yandexApi);

        using var cts = new CancellationTokenSource();

        botClient.StartReceiving(
            async (bot, update, cancellationToken) =>
            {
                if (update.Type == UpdateType.Message)
                {
                    await messageHandler.HandleMessageAsync(bot, update, cancellationToken);
                }
                else if (update.Type == UpdateType.CallbackQuery)
                {
                    await buttonHandler.HandleCallbackQueryAsync(bot, update, cancellationToken);
                }
            },
            (bot, exception, cancellationToken) => HandleErrorAsync(exception),
            cancellationToken: cts.Token
        );

        Console.WriteLine("Бот запущен. Нажмите Enter для завершения.");
        Console.ReadLine();
        cts.Cancel();
    }

    private static Task HandleErrorAsync(Exception exception)
    {
        Console.WriteLine($"Ошибка: {exception.Message}");
        return Task.CompletedTask;
    }

    private static async Task DistributionInitializing()
    {
        var scheduler = await StdSchedulerFactory.GetDefaultScheduler();
        await scheduler.Start();

        var job = JobBuilder.Create<WeatherBroadcastJob>()
            .WithIdentity("WeatherJob", "WeatherGroup")
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("WeatherTrigger", "WeatherGroup")
            .StartNow()
            .WithSimpleSchedule(x => x
                .WithIntervalInMinutes(180)
                .RepeatForever())
            .Build();

        await scheduler.ScheduleJob(job, trigger);
    }

    //private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    //{
    //    if (update.Type == UpdateType.Message)
    //    {
    //        var message = update.Message;
    //        if (message == null) return;

    //        DateTime timestamp;

    //        if (message.Text != null)
    //        {
    //            await HandleTextMessage(botClient, update, message, cancellationToken);
    //        }
    //        else if (message.Photo != null && message.Photo.Length > 0)
    //        {
    //            await HandlePhotoMessage(botClient, update, message, cancellationToken);
    //        }
    //        else if (message.Video != null)
    //        {
    //            await HandleVideoMessage(botClient, update, message, cancellationToken);
    //        }
    //        else if (message.Sticker != null)
    //        {
    //            await HandleStickerMessage(botClient, update, message, cancellationToken);
    //        }
    //    }

    //    await ButtonProcessing(botClient, update, cancellationToken);
    //}

    //private static async Task HandleStickerMessage(ITelegramBotClient botClient, Update update, Message? message, CancellationToken cancellationToken)
    //{
    //    long chatId = message.Chat.Id;

    //    Console.WriteLine($"Получен стикер: {message.Sticker.FileId} от {update.Message.Chat.FirstName} ({message.Chat.Id}) ");

    //    await botClient.SendSticker(
    //        chatId: message.Chat.Id,
    //        sticker: "CAACAgIAAxkBAAEKdApnRlzXe0mSj-2Sr--V_5jD19VHbgACGgADJeuTH6o1_iV2aiQfNgQ",
    //        cancellationToken: cancellationToken
    //    );
    //}

    //private static async Task HandleVideoMessage(ITelegramBotClient botClient, Update update, Message? message, CancellationToken cancellationToken)
    //{
    //    var couchDbService = new CouchDbService(AppConfiguration);
    //    long chatId = message.Chat.Id;

    //    Console.WriteLine($"Получено видео: {message.Video.FileId} от {update.Message.Chat.FirstName} ({message.Chat.Id}) ");

    //    await botClient.SendMessage(
    //        chatId: update.Message.Chat.Id,
    //        text: "видео мне не нужны",
    //        cancellationToken: cancellationToken
    //    );
    //    var filePath = await DownloadFileAsync(botClient, message.Video.FileId);
    //    Console.WriteLine($"Видео загружено: {filePath}");
    //    await couchDbService.SaveMessageAsync(chatId, "video", null, filePath);
    //}

    //private static async Task HandlePhotoMessage(ITelegramBotClient botClient, Update update, Message? message, CancellationToken cancellationToken)
    //{
    //    long chatId = message.Chat.Id;
    //    var couchDbService = new CouchDbService(AppConfiguration);

    //    Console.WriteLine($"Получено фото: {message.Photo[^1]} от {update.Message.Chat.FirstName} ({message.Chat.Id}) ");

    //    await botClient.SendMessage(
    //        chatId: update.Message.Chat.Id,
    //        text: "фотки мне не нужны",
    //        cancellationToken: cancellationToken
    //    );

    //    var largestPhoto = message.Photo[^1];
    //    var filePath = await DownloadFileAsync(botClient, largestPhoto.FileId);
    //    Console.WriteLine($"Фото загружено: {filePath}");

    //    await couchDbService.SaveMessageAsync(chatId, "photo", null, filePath);
    //}

    //private static async Task HandleTextMessage(ITelegramBotClient botClient, Update update, Message? message, CancellationToken cancellationToken)
    //{

    //    //if (message.Chat.Id == 568533621)
    //    //{
    //    //    await botClient.SendMessage(
    //    //        chatId: 5266940011,
    //    //        text: update.Message.Text.Trim(),
    //    //        cancellationToken: cancellationToken
    //    //    );
    //    //}
    //    //else
    //    //{
    //    long chatId = message.Chat.Id;
    //    var couchDbService = new CouchDbService(AppConfiguration);

    //    string userMessage = update.Message.Text.Trim();
    //    string userName = update.Message.Chat.FirstName ?? "Неизвестный пользователь";
    //    string logEntry = $"в {DateTime.Now} получено сообщение от {userName}" +
    //        $" ({update.Message.Chat.Id}): {userMessage}";

    //    Console.WriteLine(logEntry);
    //    await SaveMessageToFile(logEntry);

    //    string response;
    //    string dede;
    //    string deded = "";
    //    IReplyMarkup? replyMarkup = null;

    //    if (userMessage.ToLower() == "/start")
    //    {
    //        response = "Привет! Напиши название города, чтобы узнать погоду, или нажми на кнопку для быстрой отправки.";

    //        await botClient.SendMessage(
    //        chatId: update.Message.Chat.Id,
    //        text: response,
    //        cancellationToken: cancellationToken
    //        );

    //        return;
    //    }
    //    else
    //    {
    //        var weatherService = new WeatherService(AppConfiguration);
    //        response = await weatherService.GetWeatherAsync(userMessage);

    //        var yandexApi = new YandexApi(AppConfiguration);
    //        deded = await yandexApi.GetWeatherAsync(userMessage);

    //        var gismeteoService = new Gismeteo(AppConfiguration);
    //        //dede = await gismeteoService.GetWeatherAsync(userMessage);

    //        if (!response.Contains("Такого населённого пункта у нас нет :/", StringComparison.OrdinalIgnoreCase))
    //        {
    //            replyMarkup = new InlineKeyboardMarkup(new[]
    //            {
    //            new[] { InlineKeyboardButton.WithCallbackData($"{userMessage}", $"city:{userMessage}") }
    //            });
    //        }
    //    }

    //    //await botClient.SendMessage(
    //    //    chatId: update.Message.Chat.Id,
    //    //    text: $" gismeteo:\n{dede}",
    //    //    cancellationToken: cancellationToken
    //    //);

    //    await botClient.SendMessage(
    //        chatId: update.Message.Chat.Id,
    //        text: $"weather:\n{response}",

    //        cancellationToken: cancellationToken
    //    );

    //    await botClient.SendMessage(
    //        chatId: update.Message.Chat.Id,
    //        text: $" yandex:\n{deded}",
    //        replyMarkup: replyMarkup,
    //        cancellationToken: cancellationToken
    //    );



    //    await couchDbService.SaveMessageAsync(chatId, "text", message.Text, null);
    //    //}
    //}

    //private static async Task ButtonProcessing(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    //{
    //    if (update.CallbackQuery == null || update.CallbackQuery.Data == null)
    //        return;

    //    string callbackData = update.CallbackQuery.Data;
    //    IReplyMarkup? replyMarkup = null;

    //    if (callbackData.StartsWith("city:"))
    //    {
    //        string cityName = callbackData.Replace("city:", "");

    //        var yandexApi = new YandexApi(AppConfiguration);
    //        string deded = await yandexApi.GetWeatherAsync(cityName);

    //        await botClient.SendMessage(
    //            chatId: update.CallbackQuery.Message.Chat.Id,
    //            text: $" yandex:\n{deded}",
    //            cancellationToken: cancellationToken
    //        );

    //        var weatherService = new WeatherService(AppConfiguration);
    //        string weatherResponse = await weatherService.GetWeatherAsync(cityName);

    //        if (!weatherResponse.Contains("Такого населённого пункта у нас нет :/", StringComparison.OrdinalIgnoreCase))
    //        {
    //            replyMarkup = new InlineKeyboardMarkup(new[]
    //            {
    //            new[] { InlineKeyboardButton.WithCallbackData($"{cityName}", $"city:{cityName}") }
    //            });
    //        }

    //        await botClient.SendMessage(
    //            chatId: update.CallbackQuery.Message.Chat.Id,
    //            text: $"weather:\n{weatherResponse}",
    //            replyMarkup: replyMarkup,
    //            cancellationToken: cancellationToken
    //        );            
    //    }
    //}

    //private static async Task<string> DownloadFileAsync(ITelegramBotClient botClient, string fileId)
    //{
    //    try
    //    {
    //        var file = await botClient.GetFile(fileId);

    //        if (file?.FilePath == null)
    //        {
    //            throw new InvalidOperationException("Файл не найден.");
    //        }

    //        string localPath = Path.Combine("downloads", file.FilePath);

    //        Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

    //        using (var fileStream = new FileStream(localPath, FileMode.Create))
    //        {
    //            await botClient.DownloadFile(file.FilePath, fileStream);
    //        }

    //        return localPath;
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Ошибка при загрузке файла: {ex.Message}");

    //        return null!;
    //    }
    //}

    //private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    //{
    //    var errorMessage = exception switch
    //    {
    //        ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
    //        _ => exception.ToString()
    //    };

    //    Console.WriteLine(errorMessage);
    //    return Task.CompletedTask;
    //}

    //private static async Task SaveMessageToFile(string logEntry)
    //{
    //    string logFilePath = "messages_log.txt";
    //    try
    //    {
    //        await System.IO.File.AppendAllTextAsync(logFilePath, logEntry + Environment.NewLine);
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Ошибка записи в файл: {ex.Message}");
    //    }
    //}
}

public class WeatherBroadcastJob : IJob
{
    private static IConfiguration? AppConfiguration;

    public async Task Execute(IJobExecutionContext context)
    {
        AppConfiguration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var botToken = AppConfiguration["BotSettings:BotToken"];

        var couchDbService = new CouchDbService(AppConfiguration);
        var yandexService = new YandexApi(AppConfiguration);
        var botClient = new TelegramBotClient(botToken);

        var uniqueChatIds = await couchDbService.GetUniqueChatIdsAsync(AppConfiguration);
        string yandexReport = await yandexService.GetWeatherAsync("Казань");              

        try
        {
            foreach (var chatId in uniqueChatIds)
            {
                await botClient.SendMessage(chatId, $"Текущая погода в Казани:\n{yandexReport}");
                // останавливается на третьей итерации..
                Console.WriteLine(chatId);
            }
        }
        catch (Exception)
        {
            //придумать обход заблокированного собеседника

            Console.WriteLine("Собеседник заблокировал бота");
        }

        Console.WriteLine("Рассылка погоды выполнена.");
    }
}