using System;
using System.Collections.Generic;
using System.Text;

namespace ResilientWeatherGateway_Backend_Practice_2.Models
{
    public class WeatherData
    {
        public string? SourceApi { get; set; }
        public double TemperatureC { get; set; }
        public int HumidityPercent { get; set; }
        public double FeelsLikeC { get; set; }
        public string? Condition { get; set; }
        
        public DateTime RetrievedAt { get; set; }
    }
}
