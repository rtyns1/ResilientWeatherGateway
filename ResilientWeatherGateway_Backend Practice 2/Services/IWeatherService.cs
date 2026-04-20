using System;
using System.Collections.Generic;
using System.Text;
using ResilientWeatherGateway_Backend_Practice_2.Models;

namespace ResilientWeatherGateway_Backend_Practice_2.Services
{
    public interface IWeatherService
    {
        //interface
        //completely abstract class, which only contain abstract methods, and properties, essentially empty bodies.
        //Describes what a class can do, but typically does not the provide the how, the actual implementation.
        // interfaces cannot contain instance fields or variables
        Task<WeatherData> GetWeatherAsync(string city);
        // weirdly simple, but may be altered when im refactoring later.

        //nethod is async, it returns a WeatherData object.
        // parameter is string city---> both services must accept a city name.


    }
}
