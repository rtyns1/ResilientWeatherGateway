using ResilientWeatherGateway_Backend_Practice_2.Helpers;
using ResilientWeatherGateway_Backend_Practice_2.Services;
using ResilientWeatherGateway_Backend_Practice_2.Models;
using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ResilientWeatherGateway_Backend_Practice_2 .Services
{
    public class OpenWeatherMapService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly CircuitBreaker _circuitBreaker;  // circuit breaker instance if i am usin the manual circuitbreker
        private readonly string _apiKey;
        private readonly string _baseUrl;

        // OLD constructor with CircuitBreaker parameter – commented out
        // this constructor is used if i am using the manual circuit breker
         public OpenWeatherMapService(HttpClient _httpClient, string _apiKey, string _baseUrl, CircuitBreaker _circuitBreaker)
         {
             this._httpClient = _httpClient;
             this._apiKey = _apiKey;
            this._baseUrl = _baseUrl;
            this._circuitBreaker = _circuitBreaker;
         }

        // NEW constructor without CircuitBreaker. Does not use neiither manual nor polly.
        /*
        public OpenWeatherMapService(HttpClient _httpClient, string _apiKey, string _baseUrl)
        {
            this._httpClient = _httpClient;
            this._apiKey = _apiKey;
            this._baseUrl = _baseUrl;
        }
        */

        public async Task<WeatherData> GetWeatherAsync(string city)
        {
            try
            {
                string url = _baseUrl + "?q=" + city + "&units=metric&appid=" + _apiKey;

                // ----- OLD MANUAL CIRCUIT BREAKER CODE (commented out for learning) -----
                // use this if im using the mnaual circuit breker
                string JsonString = await _circuitBreaker.ExecuteAsync(async () =>
                {
                    return await RetryHandler.ExecuteWithRetry(
                        async () => await _httpClient.GetStringAsync(url),
                        maxRetries: 3
                    );
                });
                // -------------------------------------------------------------------------

                // ----- TEMPORARY DIRECT HTTP CALL (no circuit breaker) -----
                //only use this if im not using a circuit breker
                /*
                string JsonString = await _httpClient.GetStringAsync(url);
                // ------------------------------------------------------------
                */

                if (string.IsNullOrWhiteSpace(JsonString))
                {
                    throw new Exception("Received empty response from OpenWeatherMapService Api");
                }

                using JsonDocument doc = JsonDocument.Parse(JsonString);
                JsonElement root = doc.RootElement;

                if (!root.TryGetProperty("main", out JsonElement mainElement))
                {
                    throw new Exception("Unable to find 'main' object in OpenWeatherMap API response.");
                }

                if (!mainElement.TryGetProperty("temp", out JsonElement tempElement))
                {
                    throw new Exception("Unable to find temperature data in OpenWeatherMap response.");
                }

                if (!mainElement.TryGetProperty("humidity", out JsonElement humidityElement))
                {
                    throw new Exception("Unable to find humidity data in OpenWeatherMap response.");
                }
                if (!mainElement.TryGetProperty("feels_like", out JsonElement feelslikeElement))
                {
                    throw new Exception("Unable to find feels_like data in OpenWeatherMap response.");
                }

                string condition = "unknown";
                if (root.TryGetProperty("weather", out JsonElement weatherArray) && weatherArray.GetArrayLength() > 0)
                {
                    JsonElement firstWeather = weatherArray[0];
                    if (firstWeather.TryGetProperty("description", out JsonElement descElement))
                    {
                        condition = descElement.GetString() ?? "unknown";
                    }
                }

                double temperature = tempElement.GetDouble();
                int humidity = humidityElement.GetInt32();
                double feelsLike = feelslikeElement.GetDouble();

                return new WeatherData
                {
                    SourceApi = "OpenWeatherMap",
                    HumidityPercent = humidity,
                    TemperatureC = temperature,
                    Condition = condition,
                    FeelsLikeC = feelsLike,
                    RetrievedAt = DateTime.UtcNow
                };
            }
            // OLD exception catch for BrokenCircuitException (comment out if you no longer use it)
            // catch (BrokenCircuitException ex)
            // {
            //     throw new Exception("Circuit breaker open: Weather API is currently unavailable.");
            // }
            catch (HttpRequestException ex)
            {
                await FileLogger.LogErrorAsync($"OpenWeatherApiService HTTP request failed: {ex.Message}");
                throw new Exception($"Failed to call OpenWeatherApiService: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new Exception("Error parsing weather data.", ex);
            }
        }
    }
}