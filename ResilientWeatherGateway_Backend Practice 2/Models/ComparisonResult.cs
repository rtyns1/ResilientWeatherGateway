using System;
using System.Collections.Generic;
using System.Text;

namespace ResilientWeatherGateway_Backend_Practice_2.Models
{
    public class ComparisonResult
    {
        public WeatherData? WeatherFromApi1 { get; set; }
        public WeatherData? WeatherFromApi2 { get; set; }
        public double TemperatureDifference { get; set; }

        public bool ExceedsThreshold { get; set; }
    }
}
