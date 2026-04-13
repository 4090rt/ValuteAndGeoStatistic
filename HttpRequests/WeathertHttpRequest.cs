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
    public class WeathertHttpRequest
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly delegateException _delegateException;
        private readonly delegateHttpException _delegateHttpException;
        private readonly TaskCancelExceptionDelegate _taskCanceledException;
        private readonly ILogger<ParserClass> _logger;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly ILogger<WeathertHttpRequest> _httpRequestLogger;
        private readonly ParserClass _parserClass;
        private readonly IMemoryCache _memoryCache;
        private readonly CordinatsHttpRequest _cordinatsHttpRequest;

        public WeathertHttpRequest(IHttpClientFactory httpClientFactory, delegateException delegateException, delegateHttpException delegateHttpException,
            TaskCancelExceptionDelegate taskCanceledException, ILogger<ParserClass> logger, SemaphoreSlim semaphoreSlim, ILogger<WeathertHttpRequest> httpRequestLogger,
            ParserClass parserClass, IMemoryCache memoryCache, CordinatsHttpRequest cordinatsHttpRequest)
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
            _cordinatsHttpRequest = cordinatsHttpRequest;
        }

        public async Task<List<WeatherData>> CacheRequest(CancellationToken cancellation = default)
        {
            string key_cache = $"cache_key";
            List<WeatherData> oldcache = null;
            string stalekey = $"stale{key_cache}";
            if (_memoryCache.TryGetValue(key_cache, out List<WeatherData> cached))
            { 
                oldcache = cached;
                return cached;
            }
            await _semaphoreSlim.WaitAsync(cancellation);

            try
            {
                if (_memoryCache.TryGetValue(key_cache, out List<WeatherData> cached2))
                {
                    return cached2;
                }

                var fallback = Policy<List<WeatherData>>
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
                        if (_memoryCache.TryGetValue(stalekey, out List<WeatherData> stalecached))
                        {
                            _logger.LogInformation($"✅ Returning stale copy for {stalecached}");
                            return cached2;
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

                        _memoryCache.Set(key_cache, result, options);

                        var staleoptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(25));

                        _memoryCache.Set(stalekey, result, staleoptions);
                        _logger.LogInformation("✅ Cached fresh data for {CacheCode}", key_cache);
                        return result;
                    }
                    else
                    {
                        _logger.LogInformation("✅ Using cached data for {CacheCode}", key_cache);
                        return default;
                    }
                });
                return resultfallback;
            }
            catch (Exception ex)
            {
                return await _delegateException.RunDelegate(_delegateException.Delegate<WeatherData>, ex);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task<List<WeatherData>> Request(CancellationToken cancellation = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("HttpClient3");

                var resultcordinats = await _cordinatsHttpRequest.CacheRequest(cancellation).ConfigureAwait(false);

                string latitude = "";
                string longitude = "";
                foreach (var item in resultcordinats)
                {
                    latitude = item.Geo?.Latitude;
                    longitude = item.Geo?.Longitude;
                }
                if (resultcordinats != null && latitude != null && longitude != null)
                {
                    var url = BuildWeatherUrl(latitude, longitude);

                    var options = new HttpRequestMessage(HttpMethod.Get, url)
                    {
                        Version = HttpVersion.Version11,
                        VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
                    };

                    var timer = System.Diagnostics.Stopwatch.StartNew();
                    using HttpResponseMessage recpon = await client.SendAsync(options, cancellation).ConfigureAwait(false);
                    timer.Stop();
                    _httpRequestLogger.LogWarning("Weather запрос завершился за {Seconds:F2} сек", timer.Elapsed.TotalSeconds);
                    if (recpon.IsSuccessStatusCode)
                    {
                        var readrecpon = await recpon.Content.ReadAsStreamAsync().ConfigureAwait(false);

                        var parsing = await _parserClass.ParserObject<WeatherData>(readrecpon).ConfigureAwait(false);
                        return parsing;

                    }
                    else
                    {
                        _httpRequestLogger.LogError("Возникла ошибка при выпонлении запроса. посткод:" + recpon.StatusCode);
                        return new List<WeatherData>();
                    }
                }
                else
                {
                    _logger.LogWarning("Не удалось получить координаты");
                    return new List<WeatherData>();
                }
            }
            catch (TaskCanceledException ex)
            {
                return await _taskCanceledException.RunDelegate(_taskCanceledException.ExceptionMethod<WeatherData>, ex);
            }
            catch (HttpRequestException ex)
            {
                return await _delegateHttpException.RunDelegate(_delegateHttpException.Delegate<WeatherData>, ex);
            }
            catch (Exception ex)
            {
                return await _delegateException.RunDelegate(_delegateException.Delegate<WeatherData>, ex);
            }
        }

        private string BuildWeatherUrl(string latitude, string longitude)
        {
            return $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m," +
                $"relative_humidity_2m,apparent_temperature,precipitation,weather_code,wind_speed_10m,wind_direction_10m&daily=temperature_2m_max," +
                $"temperature_2m_min,precipitation_sum,weather_code&timezone=auto";
        }
    }
}
