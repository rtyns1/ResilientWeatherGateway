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

## JSONLOGGER.cs
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

**These are passive tools, they are meant to be called. They wayt for other code to write to them.Call their methods etc. Hence the name helpers.**