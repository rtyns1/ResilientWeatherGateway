This program asks two different weather websites (OpenWeatherMap and WeatherAPI) for the current temperature in a city.
It asks both at the same time. If one website fails to answer, the program stops calling it for 30 seconds (circuit breaker) 
so it does not waste time hammering a broken service. It then compares the two temperatures.
If they are very different (more than 5°C apart), it writes a warning to a JSON log file for later analysis. 
If any API call fails, it writes the error to a plain text log file for immediate debugging.


## HELPERS
2 loggers: FileLogger and JsonLogger --> Both have sepcific functions.

### 1.FileLogger.cs
Acts as an alarm bell.
Logs failures that need fixin right now.
Examples of some errors it logs:::
-API returns 500 error	2026-04-20 10:30:00 - ERROR: OpenWeatherMap returned HTTP 500
Network timeout	2026-04-20 10:32:00 - ERROR: WeatherAPI timeout after 30 seconds
Deserialization fails	2026-04-20 10:33:00 - ERROR: Failed to parse JSON from OpenWeatherMap
Circuit breaker catches unexpected exception	2026-04-20 10:35:00 - ERROR: NullReferenceException in OpenWeatherMapService

It is meant for the developer to read when soemthing goes wrong.
Format: plain text - timestamp is Hardcoded inside FileLogger.LogErrorAsync() because every error log must have a timestamp

### JSONLOGGER.cs
Acts as a Data Recorder.
Logs Events and measurments, not jus tfailures.
Examples of stuff it logs:
What it logs	Example JSON
API call succeeded	{"timestamp":"2026-04-20T10:30:00","event":"api_success","api":"OpenWeatherMap","temperature":28.5,"responseTimeMs":120}
Temperature difference exceeds 5°C	{"timestamp":"2026-04-20T10:30:01","event":"temperature_warning","api1":"OpenWeatherMap","temp1":28.5,"api2":"WeatherAPI","temp2":22.3,"difference":6.2}
Circuit breaker opened	{"timestamp":"2026-04-20T10:35:00","event":"circuit_breaker","api":"OpenWeatherMap","state":"open","failureCount":3}
Circuit breaker closed	{"timestamp":"2026-04-20T10:36:00","event":"circuit_breaker","api":"OpenWeatherMap","state":"closed","successCount":1}

It is meant to be read to analyyse patterns.
Checks stuff like How often do the APIs disagree? Which API is faster How many times did the circuit breaker open?
Format is in json where each line is a JSON obejct.
Timestamp is not hardcoded because different eventsmight need different ttimestamp formats, the caller decides.

**These are passive tools, they are meant to be called. They wait for other code to write to them.Call their methods etc. Hence the name helpers.**

Program.cs
    │
    ├── calls OpenWeatherMapService
    │         │
    │         ├── success → calls JsonLogger
    │         └── failure → calls FileLogger
    │
    ├── calls WeatherApiService
    │         │
    │         ├── success → calls JsonLogger
    │         └── failure → calls FileLogger
    │
    └── compares temperatures
              │
              └── difference > 5 → calls JsonLogger

CircuitBreaker.cs
    │
    ├── state changes (Closed → Open)
    │         └── calls JsonLogger
    │
    └── exceptions during execution
              └── calls FileLogger


Order	File	Why first
1	Models/WeatherData.cs	No dependencies. Defines what weather data looks like.
2	Models/ComparisonResult.cs	No dependencies. Defines comparison output.
3	Services/IWeatherService.cs	Interface — no implementation. Defines contract.
4	Services/CircuitBreaker.cs	No external dependencies. Can test in isolation.
5	Services/OpenWeatherMapService.cs	Depends on IWeatherService + CircuitBreaker
6	Services/WeatherApiService.cs	Same as above
7	Program.cs	Depends on everything. Write last.

// Step 1: Read configuration from appsettings.json
// Step 2: Create HttpClient (singleton)
// Step 3: Create CircuitBreaker instances
// Step 4: Create service instances
// Step 5: Call both services in parallel with Task.WhenAll
// Step 6: Compare results
// Step 7: Display to console
// Step 8: Log warnings if difference > 5

Now, order matters when we are writin the program, I already know what the helpers do. But before i start writing the Models folder,
i need to understand how everything works.
I need to understand the whole project to the core, be able to explain it.
Then, be able to explain how everything flows and how things fit in together.
So, thats exactly what i shall do.

## MODELS FOLDER
These are data containers, no logic at all.

### WeatherData.cs
- Represents the weather info returned by ONE API.
-  It will be needed because OpenWeatherMapService's API and the WeatherApiService's API both return weather data.THy need to return the same type of object 
 so that program.cs can handle them identically.
 The service classes deserialize JSON from the API into this object/data container.
 Program.cs receives 2 WeatherData objects one from each API and cmopares them.

 ### ComparisonResult.cs
 -Represents the RESULT of comparing the 2 WeatherData objects.
 -Porgram.cs gets weather from both APIs, it needs to store comparison result somewhere, both temperaturs, the diffeenece, and whether to log a warning.
 -This is where the relevance of the model classes becomes clear.


 So, the logic is that the models are Data conteiners. THy are in aformat that program.cs expect. But remember the APIS all have different formats, so the service class
 /folder is where we deal with the data formatign of the specific APIs, make them into the fromat that the dta containers expect, ship the data to the data containers, and then they are shipped to 
 the program.cs. This is a rough explanation that i am about to clear up.


 Layer	Job	What it does
Models (WeatherData.cs)	Define the destination format	"This is what we want data to look like in our program"
Services (OpenWeatherMapService.cs)	Translate API response to our format	Take messy JSON from API → put into clean WeatherData box
Program.cs	Orchestrate and display	Receives clean WeatherData boxes, compares, shows results

OpenWeatherMap API (messy JSON)
    ↓
OpenWeatherMapService.cs
    ├── calls API
    ├── receives: {"main":{"temp":28.5},"name":"Nairobi",...}
    ├── extracts: temp = 28.5
    └── creates: new WeatherData { SourceApi = "OpenWeatherMap", TemperatureC = 28.5, RetrievedAt = DateTime.Now }
    ↓
WeatherData box (clean, predictable format)
    ↓
Program.cs
    ↓
Receives box, reads TemperatureC, compares with other box

"The data containers are generic containers of the data we expect before shipping it to the program. In the service, that is where we handle the APIs themselves 
they have different structures in how data is arranged — so that is where we take this data, put it in the appropriate format we expect, put it into the data containers,
then ship it to Program.cs."

**So far, i have the necessary data containers, but im still in phase one where my only concern is temperature.**