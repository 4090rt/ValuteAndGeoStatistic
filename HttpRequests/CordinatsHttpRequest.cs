using EthernetTest.DelegateException;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ValuteAndWeatherStatistic.DelegateException;
using ValuteAndWeatherStatistic.ModelData;
using ValuteAndWeatherStatistic.Parser;

namespace ValuteAndWeatherStatistic.HttpRequests
{
    public class CordinatsHttpRequest
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly delegateException _delegateException;
        private readonly delegateHttpException _delegateHttpException;
        private readonly TaskCancelExceptionDelegate _taskCanceledException;
        private readonly ILogger<ParserClass> _logger;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly ILogger<CordinatsHttpRequest> _httpRequestLogger;
        private readonly ParserClass _parserClass;
        private readonly IMemoryCache _memoryCache;
        private readonly GeoLocationHttpRequest _geoLocationHttpRequest;

        public CordinatsHttpRequest(IHttpClientFactory httpClientFactory, delegateException delegateException, delegateHttpException delegateHttpException,
            TaskCancelExceptionDelegate taskCanceledException, ILogger<ParserClass> logger, SemaphoreSlim semaphoreSlim, ILogger<CordinatsHttpRequest> httpRequestLogger,
            ParserClass parserClass, IMemoryCache memoryCache, GeoLocationHttpRequest geoLocationHttpRequest)
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
            _geoLocationHttpRequest = geoLocationHttpRequest;
        }

        public async Task<List<GeoLocation>> CacheRequest(CancellationToken cancellation = default)
        {
            string cache_key = $"cacheley_forCordinats";
            List<GeoLocation> oldcache = null;
            string stalekey = $"stale{cache_key}";
            if (_memoryCache.TryGetValue(cache_key, out List<GeoLocation> cached))
            {
                oldcache = cached;
                return cached;
            }

            await _semaphoreSlim.WaitAsync(cancellation);
            try
            {
                if (_memoryCache.TryGetValue(cache_key, out List<GeoLocation> cached2))
                {
                    return cached2;
                }

                var fallback = Policy<List<GeoLocation>>
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
                        if (oldcache != null)
                        {
                            _logger.LogInformation("✅ Fallback: возвращаю старые данные из кэша");
                            return oldcache;
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

                var resultfallback = await fallback.ExecuteAsync(async () =>
                {
                    var result = await Request(cancellation).ConfigureAwait(false);

                    if (result != null)
                    {
                        var options = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(25))
                        .SetSlidingExpiration(TimeSpan.FromMinutes(15));
                        _memoryCache.Set(cache_key, result, options);

                        var staleoptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(25));
                        _memoryCache.Set(stalekey, result, staleoptions);
                        _logger.LogInformation("✅ Cached fresh data for {CacheCode}", cache_key);
                        return result;
                    }
                    else
                    {
                        _logger.LogInformation("✅ Using cached data for {CacheCode}", cache_key);
                        return default;
                    }
                });
                return resultfallback;
            }
            catch (Exception ex)
            {
                return await _delegateException.RunDelegate(_delegateException.Delegate<GeoLocation>, ex);
            }
            finally
            {
                _semaphoreSlim?.Release();
            }
        }

        public async Task<List<GeoLocation>> Request(CancellationToken cancellation = default)
        {
            try
            {
                var geo = await _geoLocationHttpRequest.RequestCache(cancellation).ConfigureAwait(false);
                string City = "";
                foreach (var item in geo)
                {
                    City = item.Geo?.City;
                }

                var client = _httpClientFactory.CreateClient("ClientHttp");
                var options = new HttpRequestMessage(HttpMethod.Get, $"https://api.ipgeolocation.io/timezone?apiKey=818684b83cb44c9f87e6a189bf48bf83&location={City}")
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
                };

                var timer = System.Diagnostics.Stopwatch.StartNew();
                using HttpResponseMessage recpon = await client.SendAsync(options, cancellation);
                timer.Stop();
                _httpRequestLogger.LogWarning("Сordinats запрос завершился за {Seconds:F2} сек", timer.Elapsed.TotalSeconds);

                if (recpon.IsSuccessStatusCode)
                {
                    var readrecpon = await recpon.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    var parsed = await _parserClass.ParserObject<GeoLocation>(readrecpon).ConfigureAwait(false);
                    return parsed.Where(g => g != null).ToList();
                }
                else
                {
                    _httpRequestLogger.LogError("Возникла ошибка при выпонении запроса. посткод:" + recpon.StatusCode);
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
