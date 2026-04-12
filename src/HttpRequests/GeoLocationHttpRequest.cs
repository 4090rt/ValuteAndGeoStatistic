using EthernetTest.DelegateException;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ValuteAndWeatherStatistic.DelegateException;
using ValuteAndWeatherStatistic.ModelData;
using ValuteAndWeatherStatistic.Parser;

namespace ValuteAndWeatherStatistic.HttpRequests
{
    public class GeoLocationHttpRequest
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly delegateException _delegateException;
        private readonly delegateHttpException _delegateHttpException;
        private readonly TaskCancelExceptionDelegate _taskCanceledException;
        private readonly ILogger<ParserClass> _logger;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly ILogger<GeoLocationHttpRequest> _httpRequestLogger;
        private readonly ParserClass _parserClass;
        private readonly IMemoryCache _memoryCache;

        public GeoLocationHttpRequest(IHttpClientFactory httpClientFactory, delegateException delegateException, delegateHttpException delegateHttpException,
            TaskCancelExceptionDelegate taskCanceledException, ILogger<ParserClass> logger, SemaphoreSlim semaphoreSlim, ILogger<GeoLocationHttpRequest> httpRequestLogger,
            ParserClass parserClass, IMemoryCache memoryCache)
        {
            _httpClientFactory = httpClientFactory;
            _delegateException = delegateException;
            _delegateHttpException = delegateHttpException;
            _taskCanceledException = taskCanceledException;
            _logger = logger;
            _semaphoreSlim = semaphoreSlim;
            _httpRequestLogger = httpRequestLogger;
            _parserClass = parserClass;
            _memoryCache = memoryCache;
        }

        public async Task<List<GeoLocation>> RequestCache(CancellationToken cancellation = default)
        {
            string cache_key = "cache_keyFromLocation";
            List<GeoLocation> oldcached = null;
            string stalekey = $"stale{cache_key}";
            if (_memoryCache.TryGetValue(cache_key, out List<GeoLocation> cached))
            {
                oldcached = cached;
                return cached;
            }
            await _semaphoreSlim.WaitAsync(cancellation);
            try
            {
                if (_memoryCache.TryGetValue(cache_key, out List<GeoLocation> cached2))
                {
                    return cached2;
                }

                var fallvack = Policy<List<GeoLocation>>
                    .Handle<Exception>()
                    .OrResult(r => r == null)
                    .FallbackAsync(
                    fallbackAction: async (outcome, context, ctx) =>
                    {
                        var exception = outcome.Exception;
                        var isEmpty = outcome.Result == null;

                        if (exception != null)
                        {
                            _logger.LogWarning($"⚠️ Fallback by exception: {exception.Message}");
                        }
                        if (isEmpty)
                        {
                            _logger.LogWarning($"⚠️ Fallback by empty result");
                        }
                        if (oldcached != null)
                        {

                            _logger.LogInformation("✅ Fallback: возвращаю старые данные из кэша");
                            return oldcached;
                        }
                        if (_memoryCache.TryGetValue(stalekey, out List<GeoLocation> stalecached))
                        {
                            _logger.LogInformation($"✅ Returning stale copy for {stalecached}");
                            return stalecached;
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ Fallback: кэш пуст, возвращаю default");
                            return default;
                        }
                    },
                    onFallbackAsync: async (outcome, ctx) =>
                    {
                        _logger.LogError($"🆘 Fallback сработал: {outcome.Exception?.Message}");
                        await Task.CompletedTask;
                    });

                var fallbackresult = await fallvack.ExecuteAsync(async () =>
                {
                    var result = await Request(cancellation).ConfigureAwait(false);

                    if (result != null)
                    {
                        var options = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(40))
                        .SetSlidingExpiration(TimeSpan.FromMinutes(20));

                        _memoryCache.Set(cache_key, result, options);

                        var staleooptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(40));

                        _memoryCache.Set(stalekey, result, staleooptions);
                        _logger.LogInformation("✅ Cached fresh data for {CacheCode}", cache_key);
                        return result;
                    }
                    else
                    {
                        _logger.LogInformation("✅ Using cached data for {CacheCode}", cache_key);
                        return default;
                    }
                });
                return fallbackresult;
            }
            catch (Exception ex)
            {
                return await _delegateException.RunDelegate(_delegateException.Delegate<GeoLocation>, ex);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task<List<GeoLocation>> Request(CancellationToken cancellation = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("HttpClient2");

                var options = new HttpRequestMessage(HttpMethod.Get, "https://api.ipgeolocation.io/timezone?apiKey=818684b83cb44c9f87e6a189bf48bf83&location")
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
                };

                var timer = System.Diagnostics.Stopwatch.StartNew();
                using HttpResponseMessage responce = await client.SendAsync(options, cancellation).ConfigureAwait(false);
                timer.Stop();
                _httpRequestLogger.LogWarning("GeoLocation запрос завершился за {Seconds:F2} сек", timer.Elapsed.TotalSeconds);
                    if (responce.IsSuccessStatusCode)
                    {
                        var resultread = await responce.Content.ReadAsStreamAsync().ConfigureAwait(false);

                        var resultparse = await _parserClass.ParserObject<GeoLocationResponse>(resultread).ConfigureAwait(false);

                        return resultparse.Select(r => r.Geo).Where(g => g != null).ToList();
                    }
                    else
                    {
                        _httpRequestLogger.LogError("Возникла ошибка при выпонлении запроса. посткод:" + responce.StatusCode);
                        return new List<GeoLocation>();
                    }
            }
            catch (TaskCanceledException ex)
            {
                return await _taskCanceledException.RunDelegate(_taskCanceledException.ExceptionMethod<GeoLocation>, ex);
            }
            catch (HttpRequestException ex)
            {
                return await _delegateHttpException.RunDelegate(_delegateHttpException.Delegate<GeoLocation>, ex);
            }
            catch (Exception ex)
            {
                return await _delegateException.RunDelegate(_delegateException.Delegate<GeoLocation>, ex);
            }
        }
    }
}
