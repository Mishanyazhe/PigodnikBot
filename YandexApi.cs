using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pigodnik
{
    public class YandexApi(IConfiguration configuration)
    {
        private readonly IConfiguration _configuration = configuration;

        public async Task<string> GetWeatherAsync(string city)
        {
            try
            {
                var (lat, lon) = await GetCoordinatesAsync(city);

                string apiKey = _configuration["BotSettings:YandexApiKey"];
                string apiUrl = _configuration["UrlReferens:YandexUrl"];

                if (lat == lon)
                {
                    throw new HttpRequestException();
                }

                string requestUrl = $"{apiUrl}?lat={lat}&lon={lon}";

                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("X-Yandex-API-Key", apiKey);

                HttpResponseMessage response = await client.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    string responseData = await response.Content.ReadAsStringAsync();
                    return ParseWeatherResponse(responseData);
                }
                else
                {
                    return $"Ошибка при получении данных о погоде: {response.StatusCode}";
                }
            }
            catch (HttpRequestException)
            {
                return "Такого населённого пункта у нас нет :/";
            }
        }

        public async Task<(string lat, string lon)> GetCoordinatesAsync(string city)
        {
            string geocoderApiKey = _configuration["BotSettings:YandexGeocoderApiKey"];
            string geocoderUrl = $"https://geocode-maps.yandex.ru/1.x/?apikey={geocoderApiKey}&format=json&geocode={Uri.EscapeDataString(city)}";

            using HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(geocoderUrl);

            if (response.IsSuccessStatusCode)
            {
                string responseData = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(responseData);

                var point = json["response"]["GeoObjectCollection"]["featureMember"]
                    .FirstOrDefault()?["GeoObject"]["Point"]["pos"]?.ToString();

                if (point != null)
                {
                    string[] coordinates = point.Split(' ');
                    string lon = coordinates[0];
                    string lat = coordinates[1];
                    return (lat, lon);
                }
                else
                {
                    string lat = "";
                    string lon = "";
                    return (lat, lon);
                }
            }

            throw new Exception("Не удалось получить координаты города.");
        }


        private string ParseWeatherResponse(string responseData)
        {
            JObject json = JObject.Parse(responseData);

            var temperature = json["fact"]["temp"].ToString();          // Температура
            var feelsLike = json["fact"]["feels_like"].ToString();      // Ощущаемая температура
            var windSpeed = json["fact"]["wind_speed"].ToString();      // Скорость ветра
            var humidity = json["fact"]["humidity"].ToString();         // Влажность
            var condition = json["fact"]["condition"].ToString();       // Состояние погоды

            var conditionTranslations = new Dictionary<string, string>
            {
                { "clear", "☀️ Ясно" },
                { "partly-cloudy", "🌤️ Малооблачно" },
                { "cloudy", "🌤️ Малооблачно" },
                { "overcast", "🌫️ Пасмурно" },
                { "rain", "🌧️ Дождь" },
                { "snow", "❄️ Снег" },
                { "thunderstorm", "🌩️ Гроза" },
                { "drizzle", "Морось" },
                { "light-rain", "Небольшой дождь" },
                { "moderate-rain", "Умеренный дождь" },
                { "heavy-rain", "Сильный дождь" },
                { "continuous-heavy-rain", "Длительный сильный дождь" },
                { "showers", "Ливень" },
                { "wet-snow", "🌧🌨 Дождь со снегом" },
                { "light-snow", "Небольшой снег" },
                { "snow-showers", "Снегопад" },
                { "hail", "🧊 Град" },
                { "thunderstorm-with-rain", "⛈ Дождь с грозой" },
                { "thunderstorm-with-hail", "Гроза с градом" }
            };

            string translatedCondition = conditionTranslations.ContainsKey(condition)
                ? conditionTranslations[condition]
                : condition;

            return $"Температура: {temperature}°C (ощущается как {feelsLike}°C)\n" +
                   $"Состояние: {translatedCondition}\n" +
                   $"Скорость ветра: {windSpeed} м/с\n" +
                   $"Влажность: {humidity}%";
        }

    }
}
