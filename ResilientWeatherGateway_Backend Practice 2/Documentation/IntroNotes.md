# Hints Sheet — Problem 2: Resilient Weather Gateway

## New Concepts This Problem Adds

| New concept | What it is |
|-------------|------------|
| Circuit Breaker | Stops calling a failing API temporarily |
| Structured Logging | JSON format logs (machine-readable) |
| Polly | Industry-standard library for resilience |

## What You Already Know From Challenge 1

- Async/await ✅
- HttpClient ✅
- Retries (manual, now Polly) ✅
- Parallel requests ✅
- File logging (plain text, now JSON) ✅

**You are not starting from zero. You are upgrading.**

---

## 1. Project Structure
ResilientWeatherGateway/
├── Program.cs
├── Models/
│ ├── WeatherData.cs (temperature, condition, source API name)
│ └── ComparisonResult.cs (temperatures from both, difference, warning flag)
├── Services/
│ ├── IWeatherService.cs (interface for API clients)
│ ├── OpenWeatherMapService.cs
│ ├── WeatherAPIService.cs
│ ├── CircuitBreaker.cs (manual implementation)
│ └── PollyCircuitBreaker.cs (after refactor)
├── Helpers/
│ └── JsonLogger.cs (JSON format instead of plain text)
├── appsettings.json (API keys, endpoints)
├── appsettings.Development.json (optional local overrides)
└── error.log (plain text fallback)

text

---

## 2. Order of Implementation

**DO NOT WRITE EVERYTHING AT ONCE.**

| Step | What to build | How to test |
|------|---------------|-------------|
| 1 | `JsonLogger.cs` — logs to `weather.log` in JSON format | Call `JsonLogger.Log(new { Event = "Test", Temp = 25 })` |
| 2 | `appsettings.json` — store API keys, base URLs | Read with `Microsoft.Extensions.Configuration` |
| 3 | One weather service (e.g., OpenWeatherMap) | Hardcode API key, call it, print temp |
| 4 | Second weather service (WeatherAPI) | Same pattern, different URL/response structure |
| 5 | Parallel calls with `Task.WhenAll` | Run both, compare results |
| 6 | Manual Circuit Breaker | Track failure count per API, skip if open |
| 7 | Data comparison + warning logging | If `|temp1 - temp2| > 5` → log JSON warning |
| 8 | Refactor to Polly | Replace manual retry + manual circuit breaker |

---

## 3. Circuit Breaker States (Manual Implementation)

| State | Behavior |
|-------|----------|
| **Closed** | Normal operation. Call API. Count failures. If failures >= 3 → move to **Open** |
| **Open** | Do NOT call API. Immediately return error. Start timer for 30 seconds → move to **Half-Open** |
| **Half-Open** | Allow 1 test call. If success → move to **Closed**. If fail → move back to **Open** |

---

## 4. appsettings.json Example

```json
{
  "OpenWeatherMap": {
    "ApiKey": "YOUR_KEY_HERE",
    "BaseUrl": "https://api.openweathermap.org/data/2.5/weather",
    "City": "Nairobi"
  },
  "WeatherAPI": {
    "ApiKey": "YOUR_KEY_HERE",
    "BaseUrl": "https://api.weatherapi.com/v1/current.json",
    "City": "Nairobi"
  }
}
Do not commit API keys to GitHub. Add appsettings.json to .gitignore and create appsettings.example.json with fake keys.

5. Structured Logging Example (JSON Lines format)
Each line is one JSON object:

json
{"timestamp":"2026-04-19T14:30:00","event":"api_call_success","api":"OpenWeatherMap","temperature":28.5}
{"timestamp":"2026-04-19T14:30:01","event":"api_call_success","api":"WeatherAPI","temperature":24.2}
{"timestamp":"2026-04-19T14:30:01","event":"temperature_warning","difference":4.3,"threshold":5}
{"timestamp":"2026-04-19T14:35:00","event":"circuit_breaker_opened","api":"OpenWeatherMap","failures":3}
6. APIs to Use (Free Tiers)
API	Signup	Endpoint example	Response field for temp
OpenWeatherMap	openweathermap.org	?q=Nairobi&units=metric&appid=KEY	main.temp
WeatherAPI	weatherapi.com	/current.json?q=Nairobi&key=KEY	current.temp_c
7. What to Search When Stuck
When stuck on	Search term
Reading appsettings.json in console app	"C# Console app configuration appsettings.json"
Polly WaitAndRetryAsync	"Polly WaitAndRetryAsync example C#"
Polly Circuit Breaker	"Polly CircuitBreakerAsync policy"
JSON logging to file	"C# write JSON object to file each line"
Comparing temperatures	Math.Abs(temp1 - temp2) > 5
8. The 80/20 Rule for This Problem
Focus 80% of time on	Only 20% on
Manual circuit breaker (understand the state machine)	Polly refactor
JSON logging (getting format right)	Complex error handling
Parallel API calls (Task.WhenAll)	Configuration file edge cases
9. Common Pitfalls to Avoid
Pitfall	Why it happens	Fix
API keys exposed on GitHub	Forgot .gitignore	Use appsettings.example.json + gitignore
Circuit breaker never resets	Timer not implemented	Use Task.Delay(30000) after moving to Open
JSON logs not parseable	Missing quotes or commas	Use JsonSerializer.Serialize()
Both APIs fail → program crashes	No fallback	If both fail, log error and exit gracefully
10. When to Ask for Help
Ask if	Do NOT ask if
Polly syntax is not working after reading docs	You have not read the Polly documentation
Circuit breaker state machine logic is flawed	You have not drawn the states on paper
JSON logs are malformed	You have not printed the JSON to console first
You are stuck for 45+ minutes	You have not searched the exact error message
Your First Step
Do not open your editor yet.

Draw the circuit breaker state machine on paper. Three states: Closed → Open → Half-Open → Closed

Write what causes each transition

Then write JsonLogger.cs — it is just FileLogger but with JsonSerializer.Serialize() instead of string concatenation

1. Understanding Your Manual Circuit Breaker Code
Your CircuitBreaker class implements a state machine with three states: Closed, Open, HalfOpen. Here’s how it works, line by line:

Fields
_failureThreshold = 3 – number of consecutive failures needed to open the circuit.

_openDurationSeconds = 30 – how long the circuit stays open before trying again.

_state – current state (starts Closed).

_failureCount – counts consecutive failures while in Closed state.

_openTime – timestamp when circuit moved to Open (used to calculate 30 seconds).

_lock – ensures thread safety when multiple threads call ExecuteAsync simultaneously.

_logger – a delegate (Action<string>) that logs messages (you pass Console.WriteLine or a file logger).

Constructor
Stores the logger – the circuit breaker doesn't know about JsonLogger directly; it only knows it has a function that takes a string and does something (logging).

ExecuteAsync<T> – The Main Method
This method is generic: T is the return type of the action you want to protect (e.g., string for JSON).

Step 1 – Check Open state (inside a lock)

If the circuit is Open, it calculates how long it has been open.

If 30 seconds have passed, it transitions to HalfOpen and logs it.

If less than 30 seconds have passed, it throws a BrokenCircuitException (your custom exception) – this blocks the call immediately without trying the action.

Step 2 – Try to execute the action (the delegate passed as action)

T result = await action(); – this is where your actual API call (the lambda) runs.

If it succeeds, it enters the success path inside a lock:

If state was HalfOpen, it closes the circuit (Closed) and logs.

Resets _failureCount to 0.

Returns the result.

Step 3 – Handle failure (inside catch + lock)

If the circuit is HalfOpen and the action fails, it immediately opens the circuit again and re‑throws the exception.

If the circuit is Closed, it increments _failureCount.

If _failureCount exceeds _failureThreshold (3), it changes state to Open, records _openTime, logs, and throws.

Finally, re‑throws the original exception so the caller knows the action failed.

Why the lock?
Without it, two threads could simultaneously read/update _state, _failureCount, or _openTime, causing race conditions (e.g., both seeing _failureCount = 2 and both incrementing to 3, then both trying to open the circuit). The lock ensures only one thread modifies the state at a time.

Why custom BrokenCircuitException?
It allows the caller to distinguish between “circuit is open” (do not retry, just inform user) from other exceptions like HttpRequestException (which you may want to retry). This preserves encapsulation: the caller doesn’t need to know the circuit breaker’s internal logic – just that it is open.

2. How It Relates to the Rest of Your Code
In your OpenWeatherMapService.GetWeatherAsync, you have:

csharp
string jsonString = await _circuitBreaker.ExecuteAsync(async () =>
{
    return await _httpClient.GetStringAsync(url);
});
The lambda async () => { return await _httpClient.GetStringAsync(url); } is the action delegate passed to ExecuteAsync.

The circuit breaker decides when to call that lambda: only if the circuit is not open. If open, it throws BrokenCircuitException without ever calling the HTTP request.

When the lambda succeeds (HTTP 200), the circuit breaker resets failure count and returns the JSON string.

When the lambda fails (e.g., HTTP 500 or network error), the circuit breaker increments failure count and possibly opens the circuit.

Flow in Program.cs:

Two separate circuit breaker instances (cbOpenWeather and cbWeatherApi) track failures independently for each API.

Each service instance holds its own _circuitBreaker.

Program.cs calls GetWeatherAsync on both services, and they each use their own breaker.

3. What is Polly and Why Use It Over Manual?
Polly is a .NET library that provides pre‑built resilience policies: retry, circuit breaker, timeout, fallback, etc. It's mature, extensively tested, and used in thousands of production applications.

Advantages of Polly over your manual implementation
Aspect	Manual Circuit Breaker	Polly
Thread safety	You wrote lock – it's correct but error‑prone.	Polly is proven thread‑safe.
Failure detection	Consecutive failures only.	Advanced mode: failure ratio over a time window + minimum throughput.
Integration	You must manually wrap every call.	Plugs into IHttpClientFactory automatically.
Monitoring	Manual logging only.	Built‑in events (onBreak, onReset, onHalfOpen) and built‑in metrics.
Combining policies	You would need to write composition code.	One‑line WrapAsync(retry, circuitBreaker).
Health reporting	You cannot easily expose state.	CircuitBreakerStateProvider can give current state for a /health endpoint.
Configuration flexibility	Hardcoded values.	Can load from config and change at runtime.
Production readiness	Good for learning, but not hardened.	Used in thousands of production systems.
Bottom line: Your manual implementation taught you how a circuit breaker works. Polly lets you use that knowledge without reinventing the wheel and adds features you would spend weeks implementing yourself.

4. Plan to Replace Manual with Polly (Step by Step)
4.1 Install Polly
bash
dotnet add package Polly
4.2 Create a Polly Circuit Breaker Policy (in Program.cs)
csharp
using Polly;
using Polly.CircuitBreaker;

var circuitBreakerPolicy = Policy
    .Handle<HttpRequestException>()   // failures to count
    .CircuitBreakerAsync(
        exceptionsAllowedBeforeBreaking: 3,
        durationOfBreak: TimeSpan.FromSeconds(30),
        onBreak: (ex, breakDelay) => Console.WriteLine($"Circuit broken for {breakDelay.TotalSeconds}s. {ex.Message}"),
        onReset: () => Console.WriteLine("Circuit reset."),
        onHalfOpen: () => Console.WriteLine("Circuit half-open.")
    );
4.3 Modify Your Service Classes
Remove the CircuitBreaker field and constructor parameter from OpenWeatherMapService and WeatherApiService.

Remove the using statements pointing to your old namespace.

Wrap the HTTP call with the Polly policy:

csharp
public async Task<WeatherData> GetWeatherAsync(string city)
{
    string url = _baseUrl + "?q=" + city + "&units=metric&appid=" + _apiKey;
    try
    {
        string jsonString = await circuitBreakerPolicy.ExecuteAsync(async () =>
        {
            return await _httpClient.GetStringAsync(url);
        });
        // ... parsing and return (unchanged)
    }
    catch (BrokenCircuitException)   // Polly's own BrokenCircuitException
    {
        throw new Exception("Circuit breaker open: Weather API is currently unavailable.");
    }
    catch (HttpRequestException ex)
    {
        await FileLogger.LogErrorAsync($"HTTP request failed: {ex.Message}");
        throw;
    }
}
Note: Polly’s BrokenCircuitException is in the Polly.CircuitBreaker namespace. You can catch it the same way.

4.4 Remove Manual References
Comment out or move CircuitBreaker.cs to a legacy folder.

Remove the instantiation of CircuitBreaker in Program.cs (you no longer need to create cbOpenWeather and cbWeatherApi).

Remove the circuit breaker parameter when creating service instances.

4.5 Test
Run your program. After three consecutive failures of an API, the Polly circuit breaker will open and you will see the onBreak log. After 30 seconds, a single test call will be allowed.

What You’ve Achieved
You understand the manual circuit breaker thoroughly.

You know how it fits into your project.

You know why Polly is a professional upgrade.

You have a clear, step‑by‑step plan to replace the manual code with Polly without breaking your existing logic.

Now let me know which part you want to dive deeper into, or if you want the exact code for the refactored Program.cs and service classes.