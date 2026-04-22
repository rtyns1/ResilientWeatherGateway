using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.InteropServices;


namespace ResilientWeatherGateway_Backend_Practice_2.Helpers
{
    public class ConfigurationHelper
    {
        //Config = appsettings.json + appsettings.Development.json + code that reads them.
        // It is configuring which API to call, Base URL, it is configuring my API key, and which city to query.
        // These are things that change without recompiling.
        /*
         * 
         * Create a new ConfigurationBuilder
          Set the base path to the current directory (where your .exe runs)
          Call .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
          Call .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
          Call .Build() to create the IConfiguration object
           Call .Build() to create the IConfiguration object
           Store this IConfiguration object in a field/property
         */

        private readonly IConfiguration? _configuration;//This field will hold all the settings after they are loaded

        //need a constructor--to build the configuration object and store it in the feld

        public ConfigurationHelper()// should not take any paremeters
        {
            // constructor is the only place that builds the configurtion.
            //Once it is built, it is stored in _configuration and never rebuilt again
            // Rough guide on the implementation:
            // create a new configuration builder
            // set its base path to the current directory(Directory.GetCurrentDirectory())
            // Add the JSON file "appsettings.json" required - if missng, crash
            // Add the JSON file "appsettings.Development.json" optional- if its missing, ignore
            //Build the configuration
            //store the configuration in the _configuration field

            _configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
               .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
               .Build();
        }
        public T GetValue<T>(string key)
        {
           
            return _configuration.GetValue<T>(key);
        }

        public IConfigurationSection GetSection(string key)
        {
            return _configuration.GetSection(key);
        }

    }
}
