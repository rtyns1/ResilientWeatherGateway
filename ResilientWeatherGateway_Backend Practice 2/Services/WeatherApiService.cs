using ResilientWeatherGateway_Backend_Practice_2.Helpers;
using ResilientWeatherGateway_Backend_Practice_2.Services;
using ResilientWeatherGateway_Backend_Practice_2.Models;
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
        private readonly CircuitBreaker _circuitBreaker;  // OLD manual circuit breaker – commented out for learning
        private readonly string _apiKey;
        private readonly string _baseUrl;

        // OLD constructor with CircuitBreaker parameter – commented out
        public WeatherApiService(HttpClient _httpClient, string _apiKey, string _baseUrl, CircuitBreaker _circuitBreaker)
        {
             this._httpClient = _httpClient;
           this._apiKey = _apiKey;
            this._baseUrl = _baseUrl;
            this._circuitBreaker = _circuitBreaker;
        }

        // NEW constructor without CircuitBreaker (temporary, until you integrate Polly adapter)
        /*
        public WeatherApiService(HttpClient _httpClient, string _apiKey, string _baseUrl)
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
                string url = _baseUrl + "?key=" + _apiKey + "&q=" + city + "&aqi=no";


                string JsonString = await _circuitBreaker.ExecuteAsync(async () =>
                {
                    return await RetryHandler.ExecuteWithRetry(
                        async () => await _httpClient.GetStringAsync(url),
                        maxRetries: 3
                    );
                });
                // -------------------------------------------------------------------------

                // ----- TEMPORARY DIRECT HTTP CALL (no circuit breaker) -----
                /*
                string JsonString = await _httpClient.GetStringAsync(url);
                // ------------------------------------------------------------
                */

                if (string.IsNullOrWhiteSpace(JsonString)) 
                {
                    throw new Exception("Received empty response from WeatherApiService Api");
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
                if (!current.TryGetProperty("condition", out JsonElement conditionElement))
                {
                    throw new Exception("Unable to find condition data in weatherAPI response.");
                }
                if (!current.TryGetProperty("feelslike_c", out JsonElement feelsLikeElement))
                {
                    throw new Exception("Unable to find feels like data in WeatherAPI response.");
                }

                double temperature = tempElement.GetDouble();
                int humidity = humidityElement.GetInt32();

                return new WeatherData
                {
                    SourceApi = "WeatherApiService",
                    HumidityPercent = humidity,
                    TemperatureC = temperature,
                    FeelsLikeC = feelsLikeElement.GetDouble(),
                    Condition = conditionElement.GetProperty("text").GetString(),
                    RetrievedAt = DateTime.UtcNow
                };
            }
            // OLD exception catch for BrokenCircuitException (commented out)
            catch (BrokenCircuitException ex)
            {
                 throw new Exception("Circuit breaker WeatherApiService is currently unavailable.");
            }
            /*
            catch (HttpRequestException ex)
            {
                await FileLogger.LogErrorAsync($"WeatherApiService HTTP request failed: {ex.Message}");
                throw new Exception($"Failed to call WeatherAPI: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new Exception("Error parsing weather data.", ex);
            }
            */
        }
    }
}