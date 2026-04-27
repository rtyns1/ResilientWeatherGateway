
---

## Project Structure & Build Order

| Order | File | Dependencies |
|-------|------|--------------|
| 1 | `Models/WeatherData.cs` | none |
| 2 | `Models/ComparisonResult.cs` | none |
| 3 | `Services/IWeatherService.cs` | none |
| 4 | `Services/CircuitBreaker.cs` | none |
| 5 | `appsettings.json` | none |
| 6 | `appsettings.Development.json` | none |
| 7 | `Helpers/ConfigurationHelper.cs` | `Microsoft.Extensions.Configuration` |
| 8 | `Services/OpenWeatherMapService.cs` | `IWeatherService`, `CircuitBreaker`, config |
| 9 | `Services/WeatherApiService.cs` | same as above |
| 10 | `Program.cs` | everything |

---

## Models – Data Containers, No Logic

- **`WeatherData.cs`** – defines the *destination format* that `Program.cs` expects.  
  Both services translate their specific JSON into this clean object.  
- **`ComparisonResult.cs`** – holds the result of comparing two `WeatherData` objects (temperatures, difference, warning flag).

**Separation of concerns:**  
- **Models** = what data looks like.  
- **Services** = translate API responses into models.  
- **Program.cs** = orchestrates, compares, displays.

---

## Services & Interface

**`IWeatherService.cs`** – a contract that forces every weather service to have a `GetWeatherAsync(city)` method.  
- `OpenWeatherMapService` and `WeatherApiService` implement it.  
- `Program.cs` can treat both services identically.

---

## Circuit Breaker – Manual Implementation

A **state machine** with three states:

| State | Behaviour |
|-------|-----------|
| **Closed** | Normal operation. Pass calls through; count failures. After 3 failures → move to **Open**. |
| **Open** | Block all calls immediately. Start a 30‑second timer. |
| **Half‑Open** | After 30 seconds, allow **one** test call. If it succeeds → **Closed**; if it fails → back to **Open**. |

### Data tracked internally
- current state  
- failure count (only when Closed)  
- time when circuit opened  
- a lock object (thread safety)  
- a logger delegate (`Action<string>`)

### Main method: `ExecuteAsync<T>(Func<Task<T>> action)`
- Takes a **delegate** (the API call you want to protect).  
- Runs the delegate only if the circuit is not open.  
- Updates failure counters and state based on success or failure.

### Delegate & Lambda – Simplified

- **Delegate** = a “remote control” that points to a method.  
  `Func<Task<T>>` describes a method with no parameters that returns a `Task<T>` (an async operation that eventually gives a `T`).  
- **Lambda** = a quick, unnamed method written on the spot.  
  Example: `async () => { return await _httpClient.GetStringAsync(url); }`

**Why used here?** The circuit breaker doesn’t need to know *how* to call the API – you give it a tiny lambda (the “sticky note” with instructions), and the circuit breaker executes it when it is safe.

---

## Configuration Files – `appsettings.json` & `appsettings.Development.json`

- **`appsettings.json`** – default settings (committed to Git). Contains base URLs, city name, etc.  
- **`appsettings.Development.json`** – overrides for your local machine (**ignored** by Git). Contains secret API keys.

**Why needed?**  
- Avoid hardcoding API keys (they would become public on GitHub).  
- Change city or circuit breaker threshold without recompiling.  
- Different environments (development, production) can use different settings.

**Common bug & fix:**  
The program runs from `bin/Debug/net10.0/`, but the JSON files are in the project root.  
**Fix:** Add `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>` in the `.csproj` file to copy them automatically on every build.

---

## Testing the Circuit Breaker

A fake asynchronous action that fails the first 3 times, then succeeds.

```csharp
int attempt = 0;
Func<Task<string>> fakeAction = async () =>
{
    attempt++;
    if (attempt <= 3) throw new Exception("Failure");
    return "Success";
};

The test loop calls ExecuteAsync with this fake action. The expected behaviour:

Calls 1‑3 → fail, circuit stays Closed.

After 3rd failure → circuit moves to Open.

Next call → blocked immediately (no attempt to run fake action).

After 30 seconds → one test call succeeds, circuit returns to Closed.

Putting It All Together – GetWeatherAsync (OpenWeatherMap example)
Build the URL:
url = _baseUrl + "?q=" + city + "&units=metric&appid=" + _apiKey

Call the circuit breaker with a lambda:

csharp
string jsonString = await _circuitBreaker.ExecuteAsync(async () =>
{
    return await _httpClient.GetStringAsync(url);
});
Parse JSON (defensively) using JsonDocument:

Get root, then root.GetProperty("main"), then main.GetProperty("temp").

Use TryGetProperty to avoid crashes if a field is missing.

Return a WeatherData object with the extracted temperature, humidity, feels‑like, and condition.

