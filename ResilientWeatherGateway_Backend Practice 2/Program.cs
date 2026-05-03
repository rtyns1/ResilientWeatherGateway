using ResilientWeatherGateway_Backend_Practice_2.Helpers;
using ResilientWeatherGateway_Backend_Practice_2.Models;
using ResilientWeatherGateway_Backend_Practice_2.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
// using Polly;   // Commented out because Polly section is disabled

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

                // ----- OLD MANUAL CIRCUIT BREAKER (commented out for learning) -----
                var cbOpenWeather = new CircuitBreaker(msg => Console.WriteLine(msg));
                var cbWeatherApi = new CircuitBreaker(msg => Console.WriteLine(msg));
                var openWeatherService = new OpenWeatherMapService(httpClient, openWeatherApiKey, openWeatherBaseUrl, cbOpenWeather);
                var weatherApiService = new WeatherApiService(httpClient, weatherApiKey, weatherApiBaseUrl, cbWeatherApi);


                // -------------------------------------------------------------------

                // ----- NEW POLLY CIRCUIT BREAKER (commented out for learning) -----
                // var breakerPolicy = Policy
                //     .Handle<HttpRequestException>()
                //     .CircuitBreakerAsync(
                //         exceptionsAllowedBeforeBreaking: 3,
                //         durationOfBreak: TimeSpan.FromSeconds(30),
                //         onBreak: (ex, breakDelay) => Console.WriteLine($"[CB] Open for {breakDelay.TotalSeconds}s: {ex.Message}"),
                //         onReset: () => Console.WriteLine("[CB] Closed again."),
                //         onHalfOpen: () => Console.WriteLine("[CB] Half-open – testing.")
                //     );
                // var cbOpenWeather = new PollyCircuitBreakerAdapter(breakerPolicy);
                // var cbWeatherApi = new PollyCircuitBreakerAdapter(breakerPolicy);
                // var openWeatherService = new OpenWeatherMapService(httpClient, openWeatherApiKey, openWeatherBaseUrl, cbOpenWeather);
                // var weatherApiService = new WeatherApiService(httpClient, weatherApiKey, weatherApiBaseUrl, cbWeatherApi);
                // -------------------------------------------------------------------

                // ----- TEMPORARY: DIRECT HTTP CALLS (NO CIRCUIT BREAKER) -----
                /*
                var openWeatherService = new OpenWeatherMapService(httpClient, openWeatherApiKey, openWeatherBaseUrl);
                var weatherApiService = new WeatherApiService(httpClient, weatherApiKey, weatherApiBaseUrl);
                // -------------------------------------------------------------
                */

                // Call both in parallel
                var task1 = openWeatherService.GetWeatherAsync(city);
                var task2 = weatherApiService.GetWeatherAsync(city);

                await Task.WhenAll(task1, task2);
                var weather1 = await task1;
                var weather2 = await task2;

                double diff = Math.Abs(weather1.TemperatureC - weather2.TemperatureC);

                if (diff > 2)
                {
                    await JsonLogger.LogAsync(new
                    {
                        timestamp = DateTime.UtcNow,
                        city = city,
                        weather1 = new { source = weather1.SourceApi, temp = weather1.TemperatureC },
                        weather2 = new { source = weather2.SourceApi, temp = weather2.TemperatureC },
                        difference = diff,
                        warning = "significant discrepancy between APIs detected"
                    });
                }

                Console.WriteLine($"{weather1.SourceApi}: {weather1.TemperatureC}°C, Humidity: {weather1.HumidityPercent}%, Feels like {weather1.FeelsLikeC}°C, {weather1.Condition}");
                Console.WriteLine($"{weather2.SourceApi}: {weather2.TemperatureC}°C, Humidity: {weather2.HumidityPercent}%, Feels like {weather2.FeelsLikeC}°C, {weather2.Condition}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get weather: {ex.Message}");
            }
        }
    }
}