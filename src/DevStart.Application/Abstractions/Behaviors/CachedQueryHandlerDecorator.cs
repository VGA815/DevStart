using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;
using Microsoft.Extensions.Logging;

namespace DevStart.Application.Abstractions.Behaviors
{
    internal sealed class CachingDecorator
    {
        internal sealed class QueryHandler<TQuery, TResult>(
            IQueryHandler<TQuery, TResult> innerHandler,
            ICacheService cacheService,
            ILogger<QueryHandler<TQuery, TResult>> logger)
            : IQueryHandler<TQuery, TResult>
            where TQuery : IQuery<TResult>
        {
            public async Task<Result<TResult>> Handle(TQuery query, CancellationToken cancellationToken)
            {
                if (query is not ICacheableQuery cacheableQuery)
                {
                    return await innerHandler.Handle(query, cancellationToken);
                }

                string key = cacheableQuery.CacheKey;

                TResult? cached = await cacheService.GetAsync<TResult>(key, cancellationToken);

                if (cached is not null)
                {
                    logger.LogDebug("Cache hit for {QueryType} key={Key}", typeof(TQuery).Name, key);
                    return Result.Success(cached);
                }

                logger.LogDebug("Cache miss for {QueryType} key={Key}", typeof(TQuery).Name, key);

                Result<TResult> result = await innerHandler.Handle(query, cancellationToken);

                if (result.IsSuccess && result.Value is not null)
                {
                    await cacheService.SetAsync(key, result.Value, cacheableQuery.Expiration, cancellationToken);
                }

                return result;
            }
        }
    }
}
