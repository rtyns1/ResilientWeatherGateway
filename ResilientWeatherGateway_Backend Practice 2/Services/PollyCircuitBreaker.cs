using Polly;
using Polly.CircuitBreaker;

namespace ResilientWeatherGateway_Backend_Practice_2.Services
{
    public class PollyCircuitBreakerAdapter
    {
        private readonly AsyncCircuitBreakerPolicy _policy;

        public PollyCircuitBreakerAdapter(AsyncCircuitBreakerPolicy policy)
        {
            _policy = policy;
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            return await _policy.ExecuteAsync(action);
        }

    }

}