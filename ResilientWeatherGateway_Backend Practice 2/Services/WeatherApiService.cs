using AsyncDataAggregator__Backend_practice_1.Helpers;
using ResilientWeatherGateway_Backend_Practice_2.Models;
using ResilientWeatherGateway_Backend_Practice_2.Services;
using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;



namespace ResilientWeatherGateway_Backend_Practice_2.Services
{
    public class WeatherApiService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly CircuitBreaker _circuitBreaker;
        private readonly string _apiKey;
        private readonly string _baseUrl;


        public WeatherApiService(HttpClient _httpClient, string _apiKey, string _baseUrl, CircuitBreaker _circuitBreaker)
        {
            this._httpClient = _httpClient;
            this._apiKey = _apiKey;
            this._baseUrl = _baseUrl;
            this._circuitBreaker = _circuitBreaker;
        }

        public async Task<WeatherData> GetWeatherAsync(string city)
        {
            try
            {
                string url = _baseUrl + "?key=" + _apiKey + "&q=" + city + "&aqi=no";
                string JsonString = await _circuitBreaker.ExecuteAsync<string>(async () =>   // i honestly am loosing my mind
                {
                    return await _httpClient.GetStringAsync(url);

                });
                if (string.IsNullOrWhiteSpace(JsonString))
                {
                    throw new Exception("Recieved empty response from WeatherApiService Api");
                }

                using JsonDocument doc = JsonDocument.Parse(JsonString);
                JsonElement root = doc.RootElement;
                JsonElement current = root.GetProperty("current");

                if (!current.TryGetProperty("temp_c", out JsonElement tempElement))
                {

                    throw new Exception("Unable to find temperature data in WeatherAPI response.");

                }
                if (!current.TryGetProperty("humidity", out JsonElement humidityElement))
                {
                    throw new Exception("Unable to find humidity data in WeatherAPI response.");
                }

                double temperature = current.GetProperty("temp_c").GetDouble();
                int humidity = humidityElement.GetInt32();


                return new WeatherData
                {
                    SourceApi = "WeatherApiService",
                    HumidityPercent = humidity,
                    TemperatureC = temperature,
                    RetrievedAt = DateTime.UtcNow

                };
            }
            catch (BrokenCircuitException ex)
            {
                //This is to handle scenario where circuit is open
                throw new Exception("Circuit breaker WeatherApiService is currently unavailable.");
            }

            catch (HttpRequestException ex)
            {
                // Log the error so you know what happened
                await FileLogger.LogErrorAsync($"WeatherApiService HTTP request failed: {ex.Message}");

                // Re-throw a more specific exception or just re-throw the original
                throw new Exception($"Failed to call WeatherAPI: {ex.Message}", ex);
            }


            catch (JsonException ex)
            {
                throw new Exception("Error parsing weather data.", ex);
            }

        }
    }
}
