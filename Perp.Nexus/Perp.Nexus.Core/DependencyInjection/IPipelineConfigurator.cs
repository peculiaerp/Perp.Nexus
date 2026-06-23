namespace Perp.Nexus.Core.DependencyInjection;

public interface IPipelineConfigurator
{
    IPipelineConfigurator AddRetry(int maxAttempts = 3, TimeSpan? baseDelay = null);
    IPipelineConfigurator AddCircuitBreaker(int threshold = 5, TimeSpan? resetTimeout = null);
    IPipelineConfigurator AddRateLimiting(int maxTokens = 100, TimeSpan? replenishmentPeriod = null);
    IPipelineConfigurator AddTracing();
    IPipelineConfigurator AddInboxDeduplication();
    IPipelineConfigurator AddDeadLetter();
}
