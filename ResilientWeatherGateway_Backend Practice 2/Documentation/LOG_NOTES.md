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


| Order | File | Dependencies | Notes |
|-------|------|--------------|-------|
| 1 | `Models/WeatherData.cs` | None | Define data structure |
| 2 | `Models/ComparisonResult.cs` | None | Define comparison structure |
| 3 | `Services/IWeatherService.cs` | None | Define contract |
| 4 | `Services/CircuitBreaker.cs` | None | Test with fake failures, no APIs |
| 5 | `appsettings.json` | None | Template for settings (committed to Git) |
| 6 | `appsettings.Development.json` | None | Your real API keys (NOT committed — add to .gitignore) |
| 7 | `Helpers/ConfigurationHelper.cs` (optional) | `Microsoft.Extensions.Configuration` | Reads settings from JSON files |
| 8 | `Services/OpenWeatherMapService.cs` | `IWeatherService`, `CircuitBreaker`, appsettings values | Uses API key from config |
| 9 | `Services/WeatherApiService.cs` | `IWeatherService`, `CircuitBreaker`, appsettings values | Uses API key from config |
| 10 | `Program.cs` | Everything above | Orchestrates everything |



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

## SERVICES

### IWEATHER.cs

It is an interface, it is a defines the contact btwn::
-The class tht implements it. Promises to provide certain methods
- The code that uses the class. Knows what methods are available.

Now, i have to explain this in context of my project right?
So, what does this contract mean?
Each service will sign the contract in different ways::

OpenWeatherMapService	--->"I promise I have a GetWeatherAsync method"
WeatherApiService	---->"I also promise I have a GetWeatherAsync method"
Program.cs	----> "I don't care which service you are. I know you both have GetWeatherAsync. I will call it the same way for both."

### CircuitBreaker.cs
What does this class do? waht is a circuit breaker?

-- A resiliency design pattern used to prevent an application from repeatedly trying to execute an operation
that is likely to fail, such as a failing external API or database cell.
It protects system stability by tripping/opening after a failure threshhold is met, blocking requests temporarily to allow the service to recover.

I have never written a circuitbreaker at all.
So, i need to read on how to write one, and how do i test it?

How do i write a manual circuit breaker?

Your circuit breaker is a state machine. It can only be in one of three states at any time.

**State**	   **What it means**	                                                     **What happens when a call comes in**
Closed	   Everything is normal. The API is working (or we think it is).	Let the call through. Count failures. If failures reach 3 → move to Open.
Open	   The API has failed too many times. It is probably broken.	    Block the call immediately. Do NOT try to call the API. Start a 30-second timer.
HalfOpen   30 seconds have passed. Time to test if the API recovered.	Allow ONE call through. If it succeeds → move to Closed. If it fails → move back to Open.

**tep 2: Define What Data You Need to Track**
Ask yourself: "What information does the circuit breaker need to remember between calls?"

Data	                        Why you need it
Current state	            To know if you are Closed, Open, or HalfOpen
Failure count	            To know when to open the circuit (only used in Closed state)
Time when circuit opened	To know when 30 seconds have passed (only used in Open state)
A lock object	            To prevent two threads from changing state at the same time
A logger (delegate)	        To record state changes for analysis

**Step 3: Write the Constructor**
The circuit breaker needs to know how to log messages (but should not know about JsonLogger directly).

Use a delegate (Action<string>) that the caller provides.

Pseudocode:

text
Constructor takes an Action<string> parameter called logger
Store it in a private field
That is it — nothing else in the constructor
Your job: Write the constructor. It should be very short.

Step 4: Write the Main Method Signature
The method ExecuteAsync is the only public method other classes will call.

What it does: It takes a function (delegate) that represents the API call you want to protect. It returns the same type as that function.

Pseudocode:

text
public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
{
    // All the logic goes here
}
Your job: Write this method signature. Leave the body empty for now.

Step 5: Implement State Check (Open State)
When a call comes in, the first thing you do is check the current state.

Pseudocode for Open state:

text
Lock the object (so only one thread can do this at a time)
    If current state is Open:
        Calculate how many seconds have passed since _openTime
        If 30 seconds or more have passed:
            Change state to HalfOpen
            Log "Open → HalfOpen"
        Else:
            Throw an exception saying "Circuit is open"
Your job: Write this logic inside ExecuteAsync. Use lock (_lock) { ... }.

Step 6: Try to Execute the Action
After passing the state check, you now try to call the actual API (the action delegate).

Pseudocode:

text
try
{
    T result = await action();  // This calls the actual API
    // If we get here, the API call succeeded
    // Handle success (Step 7)
}
catch (Exception ex)
{
    // If we get here, the API call failed
    // Handle failure (Step 8)
}
Your job: Write this try-catch structure.

Step 7: Handle Success
If the API call succeeded, you need to update the circuit breaker's state.

Pseudocode for success:

text
Lock the object
    If current state is HalfOpen:
        Change state to Closed
        Log "HalfOpen → Closed"
    Reset failure count to 0
Return the result (the T from the action)
Your job: Write this inside the try block, after await action().

Step 8: Handle Failure
If the API call failed, you need to update failure counts and possibly open the circuit.

Pseudocode for failure:

text
Lock the object
    If current state is HalfOpen:
        Change state to Open
        Record _openTime = current time
        Log "HalfOpen → Open"
        Throw the exception (re-throw)
    
    // If we get here, state must be Closed
    Increment failure count
    Log "Failure #X in Closed state"
    
    If failure count >= 3:
        Change state to Open
        Record _openTime = current time
        Log "Closed → Open"
    
    Throw the exception (re-throw)
Your job: Write this inside the catch block.

Step 9: Test Your Circuit Breaker
Write a temporary test in Program.cs. Do not use real APIs. Use a fake function that fails on purpose.

Pseudocode for testing:

text
Create a circuit breaker with a simple logger that prints to Console
Create a counter variable (int attempt = 0)

Define a fake action:
    Increment attempt
    If attempt <= 3:
        Throw an exception ("Fake failure")
    Else:
        Return "Success"

Loop 5 times:
    Try to call ExecuteAsync with the fake action
    Catch and print any exception
    Print the current state
    Wait 1 second between calls
Expected behavior:

Attempts 1-3: Fail, state stays Closed

After attempt 3: State changes to Open

Attempt 4: Fails immediately (circuit open), no attempt to call fake action

Attempt 5: Same

After 30 seconds (you can temporarily change _openDurationSeconds to 5 for testing): Next call succeeds, state changes back to Closed

Your job: Write this test. Run it. Fix bugs until it works.

Step 10: Add Thread Safety (You Already Have)
The lock statements you added in Steps 5, 7, and 8 are what make the circuit breaker thread-safe. Without them, two simultaneous API calls could corrupt the state.

Your job: Ensure every place that reads or writes to _state, _failureCount, or _openTime is inside a lock (_lock) block.


Now, its is paramount that u test the your code and make sure whenever possible.
Im not the best at testing, so im goign to be practicing heavily. Starting with testing the circuit breaker.
I will write the testing code and paste it here, with explanation.

**21st April 2026, Tuesday**

It has taken me an extremely long time to figure out how to write the testing for the Circuit breaker, i dont even think i have understood everything.
But, i am spending too much time here.
So, my next problem i shall have to tackle extreme testing, handle asynchronous lambda expressions, and implementing some search algorithmsand data organisation,
and also use some data structures like dicts lists , dive more into asynchronous programming, conditionals, and some intro to algorithms.
All these are to be applied to a problem. As i have done here.

THIS IS THE CIRCUITBREAKER TEST::
```csharp
using System;
using System.Threading.Tasks;
using ResilientWeatherGateway_Backend_Practice_2.Services;

namespace ResilientWeatherGateway_Backend_Practice_2
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var cb = new CircuitBreaker(msg => Console.WriteLine(msg));
            int attempt = 0;
            
            Func<Task<string>> fakeAction = async () =>
            {
                attempt++;
                if (attempt <= 3)
                {
                    throw new Exception("Failure");
                }
                return "Success";
            };

            for (int i = 0; i < 4; i++)
            {
                try
                {
                    string result = await cb.ExecuteAsync(fakeAction);
                    Console.WriteLine($"Call {i} succeeded: {result}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Call {i} failed: {ex.Message}");
                }
            }
            
            Console.WriteLine("Waiting 32 seconds...");
            await Task.Delay(32000);

            try
            {
                string result = await cb.ExecuteAsync(fakeAction);
                Console.WriteLine($"After wait, succeeded: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"After wait, failed: {ex.Message}");
            }
        }
    }
}
```

It was a pain in the ass to write, and i havent understood it fully so i will be back here.

**LOGOUT***--
--I need to write the files in order so tmrw write the appsetting.json files, then write the configurationHelper then write the rest of the service files.
Document work neatl, and heavy testing. Finish phase 1 and 2 tomorrow on the 22nd April.
the pseudocode for the OpenWeatherApiService:::


CLASS OpenWeatherMapService IMPLEMENTS IWeatherService

FIELDS:
    _httpClient (HttpClient, readonly)
    _circuitBreaker (CircuitBreaker, readonly)
    _apiKey (string, readonly)
    _baseUrl (string, readonly)

CONSTRUCTOR (httpClient, circuitBreaker, apiKey, baseUrl):
    Store each parameter in its corresponding field

METHOD GetWeatherAsync(city):
    // Step 1: Build the URL
    url = _baseUrl + "?q=" + city + "&units=metric&appid=" + _apiKey
    =
    // Step 2: Make the API call through circuit breaker
    jsonString = await _circuitBreaker.ExecuteAsync(async () =>
    {
        return await _httpClient.GetStringAsync(url);
    })
    =
    // Step 3: Extract temperature from JSON
    // You need to figure out how to parse: {"main":{"temp":28.5}}
    temperature = Extract from jsonString["main"]["temp"]
    =
    // Step 4: Return WeatherData
    RETURN new WeatherData
        SourceApi = "OpenWeatherMap"
        TemperatureC = temperature
        RetrievedAt = DateTime.UtcNow
    END OF PSEUDOCODE

**22nd April 2026**

 ### SETTINGS

--Now, why do we need to write appsettings.development.json? why do i need to write appsettings.json?
--They store settings separate from your code.
--They use JSON format, same as API responses that i have been deserializing.

appsettings.json--->	Default settings for the application (committed to Git)
appsettings.Development.json--->	Overrides for your local machine (NOT committed)

Without these files, i will have to hardcode API keys in code, which makes my API keys public, not exactly what you would want.
Changeing location = recompiling.
Different settings for different environments(not entirely what this means)


OOOh, apsettings.Production.json -- for server
appsettings.Development.json --- for me, the developer, testing.


WHAT DOES SETTINGS MEAN?
-Any value that can change without me rewriting code, any value that can differ btwn environments, environments example ineclude my laptop, vs server, vs another laptop.
-Any value that should not be hardcoded because it would be a pain to update-- will need an unecessary and painful amount of refactoring.

Some examples of settings in the context of this problem::
-API Key, City name, Base URL, CircuitBreaker failure threshold, Log file path, Database connection string.

So, now that i understand what exactly these files mean, now comes the hard part:: actually figuring how to write them.


TO SAY THAT IVE BEEN TAKEN TO THE DEPTHS OF CONFUSION WOULD BE AN UNDERSTATEMENT.
 This is a summary of wahts been done::


 We built a ConfigurationHelper class that reads settings from JSON files. The program crashed because it could not find appsettings.json at runtime.

Root cause: The JSON files were in the project root (where you edit them), but the program runs from bin/Debug/net10.0/ (where the compiled .exe lives). The files were not being copied automatically.

Solution: Added <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory> to the .csproj file. This tells .NET to copy the JSON files to the output folder every time you build.

How To Debug "File Not Found" Errors In General
Step	Action
1	Read the full error message. It tells you the exact path it is looking for.
2	Print Environment.CurrentDirectory to see where your program thinks it is running.
3	Manually check if the file exists at that path.
4	If not, figure out why the file is missing (not copied, wrong name, wrong location).
5	Fix the build process (.csproj settings) or copy files manually.
Rule: Never assume a file is where you think it is. Print the path. Verify.



LEARN HOW TO UNDESTAND ERRORS, AND I NOW TRACK ALL THE CONCEPTS I ENCOUNTER AND NEED TO READ ON IN THE CONCEPTS_LOG.md file.

ALSO, IMPLEMENTED A SUCCESFUL TESTING ROUND FOR BOTH THE API, THE CONFIGURATION AND THE CIRCUITBREAKER.
I AM FAR FROM UNDERSTANDING, I STILL CANT WRITE CODE INDEPENDENTLY 100% I NEED TO READ AND IM CONSTANTLY WRONG.
ALWAYS REMEMBER I SHOW THE FINISHED VERSION, SO YOU NEVER GET TO SEE JUST HOW MUCH I TRUGGLE TO COME UP WITH A SOLUTION.
    
**23rd April 2025**

---Delete this pseudocode after im done,
Some notes here:::
Complete Pseudocode (Step-by-Step)
text
CLASS OpenWeatherMapService IMPLEMENTS IWeatherService

FIELDS:
    _httpClient (HttpClient, readonly)
    _circuitBreaker (CircuitBreaker, readonly)
    _apiKey (string, readonly)
    _baseUrl (string, readonly)

CONSTRUCTOR (httpClient, circuitBreaker, apiKey, baseUrl):
    Store each parameter in its corresponding field

METHOD GetWeatherAsync(city):
    // Step 1: Build the URL
    url = _baseUrl + "?q=" + city + "&units=metric&appid=" + _apiKey

    // Step 2: Make the API call through circuit breaker
    // The circuit breaker expects a delegate that returns Task<string>
    jsonString = await _circuitBreaker.ExecuteAsync(async () =>
    {
        return await _httpClient.GetStringAsync(url);
    })

    // Step 3: Extract temperature from JSON
    // Parse jsonString to get the numeric value at path "main.temp"
    // You will use System.Text.Json.JsonDocument
    // Steps inside:
    //   3a: using JsonDocument doc = JsonDocument.Parse(jsonString)
    //   3b: JsonElement root = doc.RootElement
    //   3c: JsonElement main = root.GetProperty("main")
    //   3d: JsonElement tempElement = main.GetProperty("temp")
    //   3e: double temperature = tempElement.GetDouble()

    // Step 4: Return WeatherData
    RETURN new WeatherData
        SourceApi = "OpenWeatherMap"
        TemperatureC = temperature
        RetrievedAt = DateTime.UtcNow
What You Must Add (Not In Pseudocode Above)
Missing piece	Where to add it
using System.Text.Json;	Top of file
Exception handling for missing properties	Inside Step 3 (if "main" or "temp" missing, throw meaningful error)
Null check for jsonString	Before parsing
Common Mistakes To Avoid
Mistake	Why it happens	What to read
Forgetting await before _httpClient.GetStringAsync	GetStringAsync returns Task, not string	"C# await operator"
Using JsonSerializer.Deserialize<T> without a class	You do not have a class for the response	"JsonDocument vs JsonSerializer"
Accessing GetProperty on wrong element	You skipped root or main	"JsonElement GetProperty example"
Not disposing JsonDocument	Memory leak	"C# using statement for JsonDocument"


once im done with this project, i will take a shit ton of time explaining the aspects of this program, a shit ton of time Because if i skip the explanation, what have i leanr?


1. What the Circuit Breaker’s ExecuteAsync Method Does (Plain English)
Your CircuitBreaker.ExecuteAsync<T> is a protective wrapper. It takes any action (like calling a weather API) and runs it, but with safety rules:

If the circuit is Closed (normal), it runs the action. If the action fails, it counts the failure. After 3 failures, it opens the circuit.

If the circuit is Open, it blocks the action immediately (throws an exception) without even trying to call the API. This gives the failing API time to recover.

After 30 seconds, it moves to Half‑Open and allows one test call. If that test call succeeds, the circuit closes again; if it fails, it goes back to Open.

In short: ExecuteAsync is a security guard that decides whether to let your API call through or block it, based on past failures.

2. What Is a Delegate Type? (Analogy)
A delegate is like a remote control that points to a method. Instead of pressing a physical button, you “invoke” the delegate, and it runs the method it points to.

Real‑world analogy:
Imagine you have a list of tasks you want a robot to do. You write the tasks on pieces of paper: “make coffee”, “open door”, “fetch book”. You give the robot a slot where it expects a piece of paper. The robot doesn’t care what is written on the paper – it just does whatever the paper says. The paper is the delegate. The robot’s slot is the method parameter that takes a delegate.

In C#, a delegate type defines the shape of the method it can point to: what parameters it takes and what it returns. For example, Func<Task<T>> is a delegate that points to a method that takes no parameters and returns Task<T> (an async method that eventually produces a value of type T).

Why important: Delegates allow you to pass behavior (a method) as an argument to another method. That is exactly what ExecuteAsync does – you pass the API‑calling method to the circuit breaker, and the circuit breaker decides when to call it.

3. What Is a Lambda Function? (Analogy)
A lambda is a quick, unnamed method written on the spot. It’s like writing a sticky note instead of a full formal letter.

Analogy:
You need a temporary helper for a single task. Instead of hiring a full‑time employee (defining a separate named method), you just say: “Hey, do this small thing right now: add these two numbers”. That is a lambda.

In C#, a lambda looks like (parameters) => { body }. For async lambdas: async (parameters) => { await something; return result; }.

Why important: Lambdas let you write the API‑calling logic directly inside the argument to ExecuteAsync, without having to create a separate named method somewhere else. It keeps the code compact and readable.

4. How This Applies to Your GetWeatherAsync Method
In OpenWeatherMapService.GetWeatherAsync, you need to:

Build the URL.

Call _circuitBreaker.ExecuteAsync and pass it the actual HTTP request as a delegate (a lambda).

The circuit breaker will then:

Check if the circuit is open. If open, it throws without calling your lambda.

If closed or half‑open, it invokes your lambda (calls the delegate), which makes the _httpClient.GetStringAsync request.

Based on success or failure, it updates its internal state (counters, circuit state).

What your lambda must do (in English):
It must be an async lambda that takes no parameters, returns Task<string>, and inside it you await _httpClient.GetStringAsync(url) and return the resulting JSON string.

Why a lambda here? Because the circuit breaker doesn’t (and shouldn’t) know anything about HttpClient or URLs. You give it a small, self‑contained piece of work (the lambda), and it executes that work under its protection.

5. The Concept of “Passing a Function as an Argument”
You have done this before without realising it:

Action<string> in your CircuitBreaker constructor – that is a delegate (a function that takes a string and returns nothing). You gave it msg => Console.WriteLine(msg).

Now Func<Task<T>> is similar, but it returns a Task<T>.

In GetWeatherAsync, you will write:

text
string jsonString = await _circuitBreaker.ExecuteAsync( async () =>
{
    string result = await _httpClient.GetStringAsync(url);
    return result;
});
The async () => { ... } is the lambda – a function with no name. You are passing that function to ExecuteAsync. The circuit breaker will call it when it decides it is safe.

6. Summary Table
Concept	Analogy	In Your Code
Delegate	A remote control pointing to a method	Func<Task<T>> – points to an async method that returns T
Lambda	A sticky‑note with instructions	async () => { await _httpClient.GetStringAsync(url); }
ExecuteAsync	A security guard that decides when to press the remote control	It calls your lambda only if circuit is not open
Your task	Write the instructions on the sticky‑note (lambda)	Inside GetWeatherAsync, build URL, then pass the lambda to ExecuteAsync
7. What You Need to Do Now
Inside GetWeatherAsync, construct the url string using _baseUrl, city, and _apiKey.

Call _circuitBreaker.ExecuteAsync with an async lambda that:

Uses await _httpClient.GetStringAsync(url)

Returns the JSON string.

Store the result (the JSON string) in a variable.

Parse the JSON to extract the temperature (using JsonDocument or similar).

Return a new WeatherData object.

Do not worry if you don’t fully master delegates and lambdas today. You will use them many times – they become natural with repetition. For now, treat the lambda as a small block of code that you wrap in async () => { ... } and hand to the circuit breaker.

Go. Write the lambda inside GetWeatherAsync. Use the pseudocode you already have. If you get a compiler error, read the message carefully – it will guide you.