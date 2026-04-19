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