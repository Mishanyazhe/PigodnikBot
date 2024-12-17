using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using System.Threading;
using System.Threading.Tasks;
using Pigodnik;
using Telegram.Bot.Types.Enums;

public class ButtonHandler
{
    private readonly WeatherService _weatherService;
    private readonly YandexApi _yandexApi;

    public ButtonHandler(WeatherService weatherService, YandexApi yandexApi)
    {
        _weatherService = weatherService;
        _yandexApi = yandexApi;
    }

    public async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.CallbackQuery == null || string.IsNullOrEmpty(update.CallbackQuery.Data))
            return;

        string callbackData = update.CallbackQuery.Data;

        if (callbackData.StartsWith("city:"))
        {
            string cityName = callbackData.Replace("city:", "");

            string logEntry = $"в {DateTime.Now} {update.CallbackQuery.Message.Chat.FirstName}" +
            $"({update.CallbackQuery.Message.Chat.Id}) нажал на кнопку с текстом: {cityName}";
            Console.WriteLine(logEntry);
            await SaveMessage.SaveMessageToFile(logEntry);

            string weather = await _weatherService.GetWeatherAsync(cityName);
            string yandexWeather = await _yandexApi.GetWeatherAsync(cityName);

            IReplyMarkup? replyMarkup = new InlineKeyboardMarkup(new[] {
                new[] { InlineKeyboardButton.WithCallbackData($"{cityName}", $"city:{cityName}") }
            });

            await botClient.SendMessage(update.CallbackQuery.Message.Chat.Id,
                $"<u><b>Weather:</b></u> {weather}\n<u><b>Yandex:</b></u> {yandexWeather}",
                parseMode: ParseMode.Html,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);

        }
    }
}
