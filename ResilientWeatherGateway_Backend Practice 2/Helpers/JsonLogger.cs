using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

namespace ResilientWeatherGateway_Backend_Practice_2.Helpers
{
    public static class JsonLogger
    {
        private static readonly string LogFilePath = "Weather.log";

        public static async Task LogAsync(object data)
        {
            try
            {
                string jsonline = JsonSerializer.Serialize(data);
                await File.AppendAllTextAsync(LogFilePath, jsonline + Environment.NewLine);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"JSON.Logging failed : {ex.Message}");

            }

        }
    }
}
