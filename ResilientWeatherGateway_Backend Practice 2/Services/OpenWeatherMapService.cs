using ResilientWeatherGateway_Backend_Practice_2.Models;
using ResilientWeatherGateway_Backend_Practice_2.Services;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;


namespace ResilientWeatherGateway_Backend_Practice_2
{
    public class OpenWeatherMapService : IWeatherService 
    {
        private readonly HttpClient _httpClient;
        private readonly CircuitBreaker _circuitBreaker;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public OpenWeatherMapService(HttpClient _httpClient, string _apiKey, string _baseUrl, CircuitBreaker _circuitBreaker)
        {
            this._httpClient = _httpClient;
            this ._apiKey = _apiKey;
            this. _baseUrl = _baseUrl;
            this._circuitBreaker = _circuitBreaker;
            // during code review, EXPLAIN why it is necessary to use the this. keyword here.
            // safe ways to initiate in the constructor, moving beyond the beginner level.
            // why using string _baseUrl = _baseUrl is wrong , and think more of safe practices
            
        }
        public async Task<WeatherData> GetWeatherAsync(string city)
        {
            // Step 1: Build the URL
            // url = _baseUrl + "?q=" + city + "&units=metric&appid=" + _apiKey

            // Step 2: Make the API call through circuit breaker
            // The circuit breaker expects a delegate that returns Task<string>
            // jsonString = await _circuitBreaker.ExecuteAsync(async () =>
            // {
            //     return await _httpClient.GetStringAsync(url);
            // })

            // Step 3: Extract temperature from JSON
            // Use System.Text.Json.JsonDocument
            // Steps:
            //   3a: using JsonDocument doc = JsonDocument.Parse(jsonString)
            //   3b: JsonElement root = doc.RootElement
            //   3c: JsonElement main = root.GetProperty("main")
            //   3d: JsonElement tempElement = main.GetProperty("temp")
            //   3e: double temperature = tempElement.GetDouble()
            //
            // Add exception handling for missing properties (if "main" or "temp" missing, throw meaningful error)
            // Add null check for jsonString before parsing

            // Step 4: Return WeatherData
            // return new WeatherData
            // {
            //     SourceApi = "OpenWeatherMap",
            //     TemperatureC = temperature,
            //     RetrievedAt = DateTime.UtcNow
            // };


            throw new NotImplementedException(); // Remove this line when you write the implementation
        }






    }
}
    









