using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace McpResilience;

public class ResilientLookup
{
    private readonly CircuitBreakerStateProvider breakerState = new();
    private readonly ResiliencePipeline<object> pipeline;

    public ResilientLookup(ILogger<ResilientLookup> logger)
    {
        pipeline = new ResiliencePipelineBuilder<object>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<object>
            {
                ShouldHandle = new PredicateBuilder<object>().Handle<TransientBackendException>(),
                FailureRatio = 0.5,
                MinimumThroughput = 3,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(10),
                StateProvider = breakerState,
                OnOpened = args =>
                {
                    logger.LogWarning("Circuit opened for {Duration}s", args.BreakDuration.TotalSeconds);
                    return default;
                }
            })
            .AddRetry(new RetryStrategyOptions<object>
            {
                ShouldHandle = new PredicateBuilder<object>().Handle<TransientBackendException>(),
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromMilliseconds(200),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    logger.LogInformation("Retry {Attempt} after {Delay}ms", args.AttemptNumber + 1, args.RetryDelay.TotalMilliseconds);
                    return default;
                }
            })
            .Build();
    }

    public CircuitState State => breakerState.CircuitState;

    public async Task<object> LookupAsync(DrugBackend backend, string drugA, string drugB, CancellationToken cancellationToken)
    {
        try
        {
            return await pipeline.ExecuteAsync(async token => await backend.LookupAsync(drugA, drugB, token), cancellationToken);
        }
        catch (BrokenCircuitException)
        {
            return new { status = "unavailable", reason = "service_circuit_open", fallback = "consult_pharmacist" };
        }
        catch (TransientBackendException)
        {
            return new { status = "unavailable", reason = "retries_exhausted", fallback = "consult_pharmacist" };
        }
    }
}
