using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Pigodnik
{
    class Gismeteo
    {
        private readonly IConfiguration _configuration;

        public Gismeteo(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> GetWeatherAsync(string city)
        {
            try
            {
                string ApiKey = _configuration["BotSettings:GismeteoApiKey"];
                string gismeteoUrl = _configuration["UrlReferens:GismeteoUrl"];

                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("X-Gismeteo-Token", ApiKey);

                string url = $"{gismeteoUrl}?lang=ru&query={city}";

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var weatherData = JsonSerializer.Deserialize<GismeteoWeatherResponse>(responseBody);

                if (weatherData == null || weatherData.Weather == null)
                    return "Не удалось получить данные о погоде.";

                return FormatWeatherData(weatherData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при запросе к Gismeteo: {ex.Message}");
                return "Ошибка при получении данных о погоде.";
            }
        }

        private string FormatWeatherData(GismeteoWeatherResponse weatherData)
        {
            return $"Температура: {weatherData.Weather.Temperature}°C\n" +
                   $"Описание: {weatherData.Weather.Description}\n" +
                   $"Ветер: {weatherData.Weather.WindSpeed} м/с";
        }
    }


    public class GismeteoWeatherResponse
    {
        public GismeteoWeather Weather { get; set; }
    }

    public class GismeteoWeather
    {
        public float Temperature { get; set; }
        public string Description { get; set; }
        public float WindSpeed { get; set; }
    }
}
