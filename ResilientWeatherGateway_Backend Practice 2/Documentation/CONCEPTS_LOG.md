# Concepts Encountered — Resilient Weather Gateway

## 1. Async Lambda Expression

| Aspect | Description |
|--------|-------------|
| **What it is** | A lambda (inline function) that is marked `async` and returns a `Task`. |
| **Where you used it** | Circuit breaker test — `Func<Task<string>> fakeAction = async () => { ... }` |
| **Why needed** | To pass an async function as a delegate to `ExecuteAsync`. |
| **Syntax** | `async () => { await Something(); return result; }` |
| **Search term** | *C# async lambda expressions* (Microsoft Docs) |

---

## 2. `.Build()` (ConfigurationBuilder)

| Aspect | Description |
|--------|-------------|
| **What it is** | A method that compiles all added configuration sources (JSON files, environment variables, etc.) into a single `IConfiguration` object. |
| **Where you used it** | `ConfigurationHelper` constructor — `.AddJsonFile(...).Build()` |
| **Why needed** | The builder pattern: you add sources, then call `.Build()` to create the final object. |
| **Analogy** | Adding ingredients to a blender, then pressing "blend" to get a smoothie. |
| **Search term** | *ConfigurationBuilder in .NET* (Microsoft Docs) |

---

## 3. `lock` Statement (Thread Safety)

| Aspect | Description |
|--------|-------------|
| **What it is** | Prevents multiple threads from executing the same block of code simultaneously. |
| **Where you used it** | `CircuitBreaker.ExecuteAsync` — `lock (_lock) { ... }` |
| **Why needed** | To prevent race conditions when multiple threads check and change the circuit state at the same time. |
| **Search term** | *C# lock statement thread synchronization* |

---

## 4. `Func<Task<T>>` Delegate -- Heavy on this and what it means to expect a delggate


| Aspect | Description |
|--------|-------------|
| **What it is** | A delegate that represents an async function returning `Task<T>`. |
| **Where you used it** | `CircuitBreaker.ExecuteAsync<T>(Func<Task<T>> action)` |
| **Why needed** | To pass the API call (the thing you want to retry) into the circuit breaker. |
| **Search term** | *Func delegate in C#* and *Passing async methods as delegates* |

---

## 5. Nullable Reference Types (`IConfiguration?`)

| Aspect | Description |
|--------|-------------|
| **What it is** | A `?` after a type indicates that the value can be `null`. |
| **Where you used it** | `private readonly IConfiguration? _configuration` |
| **Why needed** | To tell the compiler "this field may be null temporarily, but I will set it in the constructor." |
| **Search term** | *Nullable reference types in C#* |

---

## 6. `Directory.GetCurrentDirectory()`

| Aspect | Description |
|--------|-------------|
| **What it is** | Returns the full path of the directory where the program is running. |
| **Where you used it** | `ConfigurationHelper` — `SetBasePath(Directory.GetCurrentDirectory())` |
| **Why needed** | To tell `ConfigurationBuilder` where to look for the JSON files. |
| **Search term** | *Environment.CurrentDirectory vs Directory.GetCurrentDirectory()* |

---

## 7. `dotnet add package` (NuGet)

| Aspect | Description |
|--------|-------------|
| **What it is** | Command to add external libraries to your project. |
| **Where you used it** | `Microsoft.Extensions.Configuration` and related packages. |
| **Why needed** | To use built-in .NET configuration features (not in the default console template). |
| **Search term** | *NuGet packages in .NET* |

---

## 8. Build Events / File Copying (`.csproj`)

| Aspect | Description |
|--------|-------------|
| **What it is** | Instructions in the project file that control what happens during build. |
| **Where you used it** | `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>` |
| **Why needed** | To automatically copy JSON config files to the output folder. |
| **Search term** | *Copy to output directory in .NET projects* |

---

## 9. Testing Pattern (Circuit Breaker Test)

| Aspect | Description |
|--------|-------------|
| **What you did** | Created a fake async action that fails 3 times, then succeeds. Used a loop to call it multiple times. |
| **Why this works** | Isolates the circuit breaker logic from real APIs. Tests state transitions (Closed → Open → HalfOpen → Closed). |
| **Search term** | *Unit testing with fake delegates in C#* |

DEALING WITH JSON, PARSING, ADN FUNCTIONS LIKE   if (!root.TryGetProperty("main", out JsonElement mainElement) ||
                !mainElement.TryGetProperty("temp", out JsonElement tempElement))

graceful error handling, defensive programming and error handling that does not break encapsulation.
