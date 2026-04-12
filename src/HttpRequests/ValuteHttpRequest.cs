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
    public class ValuteHttpRequest
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly delegateException _delegateException;
        private readonly delegateHttpException _delegateHttpException;
        private readonly TaskCancelExceptionDelegate _taskCanceledException;
        private readonly ILogger<ParserClass> _logger;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly ILogger<ValuteHttpRequest> _httpRequestLogger;
        private readonly ParserClass _parserClass;
        private readonly IMemoryCache _memoryCache;

        public ValuteHttpRequest(IHttpClientFactory httpClientFactory, delegateException delegateException, delegateHttpException delegateHttpException,
            TaskCancelExceptionDelegate taskCanceledException, ILogger<ParserClass> logger, SemaphoreSlim semaphoreSlim, ILogger<ValuteHttpRequest> httpRequestLogger,
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

        public async Task<List<CourceData>> RequestCache(CancellationToken cancellation = default)
        {
            string cache_key = "cache_keyFromValute";
            List<CourceData> oldcached = null;
            string stalekey = $"stale{cache_key}";
            if (_memoryCache.TryGetValue(cache_key, out List<CourceData> cached))
            { 
                oldcached = cached;
                return cached;
            }
            await _semaphoreSlim.WaitAsync(cancellation);
            try
            {
                if (_memoryCache.TryGetValue(cache_key, out List<CourceData> cached2))
                {
                    return cached2;
                }

                var fallback = Policy<List<CourceData>>
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
                        if (_memoryCache.TryGetValue(stalekey, out List<CourceData> stalecached))
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
                    onFallbackAsync: async (outcome,ctx) =>
                    {
                        _logger.LogError($"🆘 Fallback сработал: {outcome.Exception?.Message}");
                        await Task.CompletedTask;
                    });

                var fallbackresult = await fallback.ExecuteAsync(async () => 
                {
                    var result = await Request(cancellation).ConfigureAwait(false);

                    if (result != null)
                    {
                        var options = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(40))
                        .SetSlidingExpiration(TimeSpan.FromMinutes(10));

                        _memoryCache.Set(cache_key, result, options);

                        var staleoptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(40));

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
                return fallbackresult;
            }
            catch (Exception ex)
            {
                return await _delegateException.RunDelegate(_delegateException.Delegate<CourceData>, ex);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task<List<CourceData>> Request(CancellationToken cancellation = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ClientHttp");

                var options = new HttpRequestMessage(HttpMethod.Get, "https://v6.exchangerate-api.com/v6/cf64a04e84d8235680fdfa09/latest/RUB")
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
                };

                var timer = System.Diagnostics.Stopwatch.StartNew();
                using HttpResponseMessage recpon = await client.SendAsync(options, cancellation).ConfigureAwait(false);
                timer.Stop();
                _httpRequestLogger.LogWarning("Valute запрос завершился за {Seconds:F2} сек", timer.Elapsed.TotalSeconds);

                if (recpon.IsSuccessStatusCode)
                {
                   var resultread = await recpon.Content.ReadAsStreamAsync().ConfigureAwait(false);

                   var parsingresult = await _parserClass.ParserObject<CourceData>(resultread).ConfigureAwait(false);
                   _httpRequestLogger.LogInformation("Распаршено {Count} записей валюты", parsingresult?.Count ?? 0);
                   return parsingresult;
                }
                else
                {
                   _httpRequestLogger.LogError("Возникла ошибка при выпонлении запроса. посткод:" + recpon.StatusCode);
                   return new List<CourceData>();
                }
            }
            catch (TaskCanceledException ex)
            {
                return await _taskCanceledException.RunDelegate(_taskCanceledException.ExceptionMethod<CourceData>, ex);
            }
            catch (HttpRequestException ex)
            {
                return await _delegateHttpException.RunDelegate(_delegateHttpException.Delegate<CourceData>, ex);
            }
            catch (Exception ex)
            {
                return await _delegateException.RunDelegate(_delegateException.Delegate<CourceData>, ex);
            }
        }
    }
}
