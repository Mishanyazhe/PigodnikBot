using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace Pigodnik
{
    public class WeatherService
    {
        private readonly IConfiguration _configuration;
        

        public WeatherService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<string> GetWeatherAsync(string city)
        {
            string ApiKey = _configuration["BotSettings:WeatherApiKey"];
            string weatherUrl = _configuration["UrlReferens:WeatherUrl"];

            using var httpClient = new HttpClient();            
            string url = $"{weatherUrl}?q={city}&units=metric&appid={ApiKey}&lang=ru";

            try
            {
                var weatherResponse = await httpClient.GetFromJsonAsync<WeatherResponse>(url);

                if (weatherResponse != null)
                {
                    return $"Температура: {weatherResponse.Main.Temp}°C\n" +
                           $"Состояние: {weatherResponse.Weather[0].Description}.\n" +
                           $"Скорость ветра: {weatherResponse.Wind.Wind_speed} м/с.";
                }
                else
                {
                    return "Не удалось получить данные о погоде.";
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Ошибка HTTP-запроса: {ex.Message}");
                return "Такого населённого пункта у нас нет :/";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка: {ex.Message}");
                return "Произошла ошибка при получении данных о погоде.";
            }
        }
    }

    public class WeatherResponse
    {
        public required Main Main { get; set; }
        public required Wind Wind { get; set; }
        public required List<Weather> Weather { get; set; }
    }

    public class Main
    {       
        public double Temp { get; set; }
    }

    public class Wind
    {
        public float Wind_speed { get; set; }
    }

    public class Weather
    {
        public required string Description { get; set; }
    }
}
