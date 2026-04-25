using AsyncDataAggregator__Backend_practice_1.Helpers;
using ResilientWeatherGateway_Backend_Practice_2.Models;
using ResilientWeatherGateway_Backend_Practice_2.Services;
using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
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
            try
            {
                string url = _baseUrl + "?q=" + city + "&units=metric&appid=" + _apiKey; //this builds the Url,

                string JsonString = await _circuitBreaker.ExecuteAsync<string>(async () =>   // i honestly am loosing my mind
                {
                    return await _httpClient.GetStringAsync(url);

                });

                // when u are programming, you should always try to anticipate for errors and when things could go wrong and how to handle those things when they go wrong

                if (string.IsNullOrWhiteSpace(JsonString))
                {
                    throw new Exception("Recieved empty response from OpenWeatherMapService Api");
                }
                
                using JsonDocument doc = JsonDocument.Parse(JsonString);//this line parses a Json string into a structured, read only document object model, that allows you to efficiently navigate and extract data without deserializing.
                // in short, converts raw Json string into a structured readonly object(JsonDocument) that you can query and navigate efficiently.
                JsonElement root = doc.RootElement;
                /*
                 * when i calll JsonDocument.Parse(JsonString), it builds an in memory represntation of the JSON. The RootElement is the stating point, the very first object or array in the JSON object,
                 * so root point to that entire object.
                 * Like a tree:: RootElement is the trunk. FRom there, u branch out to properties like "main" and "tempt"
                 * 
                 */

                // we need to again, think proactively, about error handling.

                /*
                 * JsonElement is a type that represents one node in the JSON tree. t could be an object, array, string ,number etc.
                 * mainElement is the node that correspinds to "main" property of the root object. in the OpenWeatherMap response it looks something like this::
                 * {
                              "main": {
                          "temp": 28.5
                        }
                      }
                 *So mainElement will hold the object {" temp": 28.5}
                 *
                 */


                // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

                /*
                 * below is a defensive error handling case. it tries to get the propert "main" from the rot object. If the propert exists,
                 * TryGetPropery returns true and sets mainElement to that node. If it does not, for example in the cases where the API changed its response format or there was an error message instead of weather data, it returns false.
                 *  the ccondition "!root.TryGetProperty("main", out mainElement) || !mainElement.TryGetProperty("temp", out tempElement)" means that::
                 *  if we cannot find "main or if we cannot find "temp " isnide whatever mainElement we got, then we throw an exception with a clear message.
                 *  This prevents code from crashing with cryptic errors like KeyNotFoundException or NullReferenceException.  It also allows us ti log and handle the problems more gracefully.
                 *  C# is a blessing for defensive programming and type safety enthusiasts.
  -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                 *  
                 */
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
                    // Option 1: throw an exception if humidity is required
                    throw new Exception("Unable to find humidity data in OpenWeatherMap response.");
                    
                }

                double temperature = tempElement.GetDouble();
                int humidity = humidityElement.GetInt32();


                return new WeatherData
                {
                    SourceApi = "OpenWeatherMap",
                    HumidityPercent = humidity,
                    TemperatureC = temperature,
                    RetrievedAt = DateTime.UtcNow

                };

            }

            catch (BrokenCircuitException ex)
            {
                //This is to handle scenario where circuit is open
                throw new Exception("Circuit breaker open: Weather API is currently unavailable.");
            }

            catch (HttpRequestException ex)
            {
                // Log the error so you know what happened
                await FileLogger.LogErrorAsync($"OpenWeatherApiService HTTP request failed: {ex.Message}");

                // Re-throw a more specific exception or just re-throw the original
                throw new Exception($"Failed to call OpenWeatherApiService: {ex.Message}", ex);
            }


            catch (JsonException ex)
            {
                throw new Exception("Error parsing weather data.", ex);
            }

   
        }

    }
}
    









