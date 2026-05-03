using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AsyncDataAggregator__Backend_practice_1.Services
{
    public class RetryHandler
    {
        public static async Task<T> ExecuteWithRetry<T>(Func<Task<T>> action, int maxRetries = 5, int delaymilliseconds = 1000)
        {
 
            for (int retryCount = 0; retryCount < maxRetries; retryCount++)
            {
                try
                {
                    T result = await action();
                    return result;
                }
                catch (Exception e)
                {
                    if (retryCount == maxRetries - 1)
                    {
                        throw new Exception($"Operation failed after {maxRetries}", e);
                    }
                    else
                    {
                        int delayInSeconds = (int)Math.Pow(2, retryCount + 1);
                        int delayInMilliseconds = delayInSeconds * 1000;
                        await Task.Delay(delayInMilliseconds);

                    }
                }
            }
            throw new Exception($"Operation failed after {maxRetries} retries.");
        }
    }
}
