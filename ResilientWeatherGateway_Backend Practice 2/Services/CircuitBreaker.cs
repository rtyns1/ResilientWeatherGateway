using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ResilientWeatherGateway_Backend_Practice_2.Services
{
    public enum Circuitstate
    {
        Closed, // Normal: calls go through , we count failures
        Open, // Failed too many times : blocks all calls
        HalfOpen // Testing :allows one call to see if an API recovered
    }

    public class CircuitBreaker 
    {
        private readonly int _failureThreshold = 3;
        private readonly int _openDurationSeconds = 30;
        private Circuitstate _state = Circuitstate.Closed;
        private int _failureCount = 0;
        private DateTime? _openTime = null;
        private readonly object _lock = new object();
        private readonly Action<string> _logger;

        public CircuitBreaker(Action<string> logger)
        {
            _logger = logger;
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            //This method is the only way i can call an API through the circuit breaker.
            /*
             * it decides if an API call should be let thru--- depending in if the circuit is open or closed, or if 30 seconds have passed
             * It decides if a test call should be left through -- If circuit is HalfOpen-yes for one call only
             * Decides if a failure should be counted --- if circuit is closed then yed
             * It decides if the ciruit should be opened -- if 3 failures happen in a row -- then yes
             * So, i need to go step by step and really understnd what im meant to do in this class.
             * I hav never written a circuitbreaker before, first time hearing of it.
             * 
             */
            // State check--
            //when a call comes in, first u check the current state--if circuit is open then u have to decide:
            // if 30 seconds have passed -->move to HakfOpen(allow test call)
            //If 30 seconds have not passed --> throw an exception immediately and block the call
            //Do not handle closed and halfopen states, those are handled later.
            lock (_lock) // lock in this case means 
            {
                /*
                 * It ensures only one call modifies the state at a time. Multiple calls can
                 * still happen in parallel,
                 * but they queue up at the lock statement.
                 */
                if (_state == Circuitstate.Open) // check if the state is open
                {
                    //check if 30 seconds have passed
                    if (_openTime.HasValue && (DateTime.UtcNow - _openTime.Value).TotalSeconds >= _openDurationSeconds)
                    {
                        //if yes, move to HalfOpen
                        _state = Circuitstate.HalfOpen;
                        _logger($"Circuit state changed : open --> HalfOpen");
                    }
                    else
                    {
                        //otherwise, if the circuit is still open, throw exception immediately.
                        throw new BrokenCircuitException($"Circuit is OPEN for {_openDurationSeconds} seconds. Call blocked."); 
                    }
                }
            //step 2: try to executr the API call action
            }

            try
            {
                T result = await action(); //This is where the ctual API call happens
                //step 3 is success, so handle success based on current state.
                lock (_lock)
                {
                    if (_state == Circuitstate.HalfOpen)
                    {
                        _state = Circuitstate.Closed;
                        _logger($"Circuit state changed: HalfOpen --> Closed");

                    }
                    // reset the failure count
                    _failureCount = 0;
                }
                return result;

            }
            catch (Exception ex)
            {
                //step 4: Failure, handle failure based on current state.
                /*
                 * Failure in halfopen means the circuit goes back to being open
                 * write the logger that test has failed and reflect tha change in state.
                 * rethrow original exception, like a try again thing
                 * incrment failure count ++
                 * logger info message on failure count,and how many errors in closed state plus ex.message
                 * if the failure count is greter than the threshold we had,
                 * then,open the state so _state = Cs.Open
                 * then, set _openTime  to DateTIme,UtcNoe,
                 * then write another message, logger message, say that state changed from closed to open after _failurecount failures
                 * throw orignial exception
                 * 
                 */
                lock (_lock)
                {
                    if (_state == Circuitstate.HalfOpen)
                    {
                        _state = Circuitstate.Open;
                        _logger($"Failure #{_failureCount} in Closed state: {ex.Message}");

                        throw;


                    }
                    _failureCount++;
                    _logger($"Failure {_failureCount} in closed state: {ex.Message}");

                    if (_failureCount > _failureThreshold)
                    {
                        _state = Circuitstate.Open;
                        _openTime = DateTime.UtcNow;

                        _logger($"Circuitstate changed: closed --> open after {_failureCount} failures");
                        throw;
                    }
                }
                throw;

            }
        }
    }

    public class BrokenCircuitException: Exception
    {
        public BrokenCircuitException() { }
        public BrokenCircuitException(string message) : base(message) { }
        public BrokenCircuitException (string message, Exception inner): base(message, inner) { }

    }
    /*
     * in the OpenWeatherMapService when i call _circuitBreaker.ExecuteAsync, 3 different kinds of failures can happen:
     * BrokenCircuitException	The circuit is open (API has failed too many times recently).	Do NOT retry immediately. Wait, or inform the user that the service is temporarily unavailable.
       HttpRequestException	Network error, timeout, API returned 500.	Retry (the circuit breaker will count this failure).
       JsonException	The API returned malformed JSON (maybe an error page instead of weather data).	Log and fail – retrying won't help.
    *A generic exception is not enough, we cannot distinguish these cases.
    *You have to parse error messages, or rely on generic catches that hide bugs.
    *But with the BrokenCircuitException, we cn write the followin in the API classes:::
    *catch (BrokenCircuitException)
{
    // Circuit is open – do not retry, just tell user "service unavailable"
}
catch (HttpRequestException)
{
    // Retry logic (circuit breaker already tracks failures)
}
catch (JsonException)
{
    // Log and abort – API returned garbage
}
    *CircuitBreaker has a specific job to protect external calls from hammering a failing service. IT communicates it state through exceptions. by thwoging BrokenCircuitException, it says clearly :"I am blockin this call because the cirtuit is open"
    *The caller does not nee to know how the circuit breaker works, only that it is open.
    *Separation of concerns and responsibilities at its finest.
    *Throwng generic exceptions mixes concerns and the caller cannot distinguish btw circuit is open and the API has returnd a 500 error. It breaks encapsulation in a way.
    *Encapsulation – the circuit breaker's internal state is communicated without exposing its internal logic.
    *Maintainability – future developers will immediately understand what BrokenCircuitException means, whereas a generic Exception("circuit open") can be overlooked or misinterpreted.
    *Testing – you can unit test the handling of each failure type independently.
    *
    */


}
