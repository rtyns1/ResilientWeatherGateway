Problem 2: Resilient Weather Gateway (Iterative Version)
Concept Focus
Circuit Breaker Pattern · Parallel Requests · Structured Logging · Iterative Feature Addition
Overall Goal
Build a console app that fetches weather data from two different weather APIs simultaneously, compares their results, 
and implements a circuit breaker that temporarily stops calling an API if it fails repeatedly. You will build this in phases — 
each phase adds one new capability, with a working commit at the end of each.

Phase 1: Temperature Only (Minimum Viable Product)
Goal: Get temperature from both APIs, compare, and log warnings if the difference exceeds 5°C.
What you build:

WeatherData.cs — fields: SourceApi, TemperatureC, RetrievedAt
Circuit breaker (manual implementation) — tracks failures per API
FileLogger.cs (plain text) — logs API failures
JsonLogger.cs (JSON format) — logs temperature warnings
appsettings.json — API keys and city name

Success criteria:

Program runs and fetches temperature from both APIs
Displays both temperatures to the console
If difference > 5°C, writes a JSON warning to weather.log
If an API fails 3 times, circuit breaker opens (stops calling for 30 seconds)

Commit: git commit -m "v1.0 - Temperature only working"

Phase 2: Add More Weather Fields
Goal: Extend the program to include humidity, condition, and feels-like temperature.
What you add:

WeatherData.cs — add HumidityPercent, Condition, FeelsLikeC
Update both API services to deserialize these new fields
Update comparison logic — compare each field and optionally log which fields disagree

Success criteria:

Program displays temperature, humidity, condition, and feels-like from both APIs
JSON logs include all fields

Commit: git commit -m "v1.1 - Added humidity, condition, feels-like"

Phase 3: Full Data Comparison & Analysis
Goal: Compare all weather fields and generate a detailed comparison report.
What you add:

ComparisonResult.cs — holds both API results and differences per field
Log a summary JSON object containing all differences
If any field differs significantly (e.g., humidity differs by >20%), log a warning

Success criteria:

Program outputs which fields agree and which disagree
weather.log contains a comparison summary JSON object

Commit: git commit -m "v1.2 - Full data comparison"

Phase 4: Refactor to Polly
Goal: Replace your manual circuit breaker and retry logic with the Polly library.
What you change:

Remove (or archive) manual CircuitBreaker.cs
Add the Polly NuGet package
Use Policy.Handle<HttpRequestException>().CircuitBreakerAsync(...)
Use Policy.Handle<HttpRequestException>().WaitAndRetryAsync(...)

Success criteria:

Program still works exactly the same as before
Codebase has fewer lines
You understand why Polly is preferable for production use

Commit: git commit -m "v2.0 - Refactored to use Polly"

Phase 5: Advanced Analysis (Optional / Stretch)
Goal: Use the JSON logs to answer questions about API reliability.
What you build (separate script or program):

Read weather.log (JSON lines format)
Calculate which API is more accurate (compared to the average of both), which API responds faster (requires logging response time), 
and how often the circuit breaker opens

Success criteria:
