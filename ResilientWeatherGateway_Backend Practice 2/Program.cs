using ResilientWeatherGateway_Backend_Practice_2.Helpers;
using ResilientWeatherGateway_Backend_Practice_2.Models;
using ResilientWeatherGateway_Backend_Practice_2.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;



namespace ResilientWeatherGateway_Backend_Practice_2
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ConfigurationHelper();

            string city = config.GetValue<string>("City");
            string openWeatherBaseUrl = config.GetValue<string>("OpenWeatherMap:BaseUrl");
            string weatherApiBaseUrl = config.GetValue<string>("WeatherAPI:BaseUrl");
            string openWeatherApiKey = config.GetValue<string>("OpenWeatherMap:ApiKey");
            string weatherApiKey = config.GetValue<string>("WeatherAPI:ApiKey");

            
            try
            {
                var httpClient = new HttpClient();


                var cbOpenWeather= new CircuitBreaker(msg => Console.WriteLine(msg));
                var openWeatherService = new OpenWeatherMapService(httpClient, openWeatherApiKey, openWeatherBaseUrl, cbOpenWeather);


                var cbWeatherApi = new CircuitBreaker(msg => Console.WriteLine(msg));
                var weatherApiService = new WeatherApiService(httpClient, weatherApiKey, weatherApiBaseUrl, cbWeatherApi);

                //call bith in parallel and return the first succesful result
                var task1 = openWeatherService.GetWeatherAsync(city);
                var task2 = weatherApiService.GetWeatherAsync(city);

                await Task.WhenAll(task1, task2);
                var weather1 = await task1;
                var weather2= await task2;

                double diff = Math.Abs(weather1.TemperatureC - weather2.TemperatureC);

                if (diff > 2)
                {
                    //log to JsonLogger, gives structured warning with details about the discrepancy
                    await JsonLogger.LogAsync(new{


                        timestamp = DateTime.UtcNow,
                        city = city,
                        weather1 = new {source = weather1.SourceApi, temp = weather1.TemperatureC},
                        weather2 = new {source = weather2.SourceApi, temp = weather2.TemperatureC},
                        difference = diff,
                        warning = "significant discrepance between APIs detected"


                    });
                }
                Console.WriteLine($"{weather1.SourceApi}: {weather1.TemperatureC}°C");
                Console.WriteLine($"{weather2.SourceApi}: {weather2.TemperatureC}°C");




            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get weather: {ex.Message}");
            }
            
        }
    }

}