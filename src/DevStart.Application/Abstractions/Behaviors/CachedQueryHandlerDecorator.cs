using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;

namespace DevStart.Application.Abstractions.Behaviors
{
    internal sealed class CachingDecorator
    {
        internal sealed class QueryHandler<TQuery, TResult>
            : IQueryHandler<TQuery, TResult>
            where TQuery : IQuery<TResult>
        {
            private readonly IQueryHandler<TQuery, TResult> _handler;
            private readonly ICacheService _cache;

            public QueryHandler(
                IQueryHandler<TQuery, TResult> innerHandler,
                ICacheService cacheService)
            {
                _cache = cacheService;
                _handler = innerHandler;
            }
            public async Task<Result<TResult>> Handle(TQuery query, CancellationToken cancellationToken)
            {
                if (query is not ICacheableQuery cacheableQuery)
                {
                    return await _handler.Handle(query, cancellationToken);
                }

                var key = cacheableQuery.CacheKey;

                var cached = await _cache.GetAsync<TResult>(key);

                if (cached != null) return cached;

                var result = await _handler.Handle(query, cancellationToken);

                await _cache.SetAsync(key, result, cacheableQuery.Expiration!.Value);

                return result;
            }
        }
    }
}
