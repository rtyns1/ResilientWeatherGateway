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
            var cb = new CircuitBreaker(msg => Console.WriteLine(msg));
            int attempt = 0;
            Func<Task<string>> fakeAction = async () => //asynchronous lambda expression assigned to a genric delegate.
            {
                attempt++;
                if (attempt <= 3)
                {
                    throw new Exception("Failure");
                    // wtf do u mean return the string success

                }
                return "Success";
               

            };

            for (int i=0; i< 6; i++)
            {
                try
                {
                    string result = await cb.ExecuteAsync(fakeAction);
                    Console.WriteLine($"Call {i} succeeded: {result}");

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Call {i} failed: {ex.Message}");

                }
            }
            Console.WriteLine("Waiting 32 seconds...");
            await Task.Delay(32000);

            try
            {
                string result = await cb.ExecuteAsync(fakeAction);
                Console.WriteLine($"After wait, succeeded: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"After wait, failed: {ex.Message}");
            }

            var config = new ConfigurationHelper();

            string city = config.GetValue<string>("City");
            string openWeatherBaseUrl = config.GetValue<string>("OpenWeatherMap:BaseUrl");
            string openWeatherApiKey = config.GetValue<string>("OpenWeatherMap:ApiKey");

            HttpClient _httpClient = new HttpClient();
            try
            {
                var httpClient = new HttpClient();
                var circuitBreaker = new CircuitBreaker(msg => Console.WriteLine(msg));
                var cb2 = new CircuitBreaker(msg => Console.WriteLine(msg));

                var weatherService = new OpenWeatherMapService(_httpClient, openWeatherApiKey, openWeatherBaseUrl, cb2);
                WeatherData weather = await weatherService.GetWeatherAsync(city);
                Console.WriteLine($"Temperature in {city}: {weather.TemperatureC} in degrees celsius.");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get weather: {ex.Message}");
            }
            
        }
    }

}