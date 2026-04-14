using EthernetTest.DelegateException;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValuteAndWeatherStatistic.DelegateException;
using ValuteAndWeatherStatistic.ModelData.ModelDataSelectRequest;

namespace ValuteAndWeatherStatistic.DataBase.SelectRequest
{
    public class SelectWratherRequest
    {
        private readonly ILogger<SelectWratherRequest> _logger;
        private readonly PoolSQLiteConnect _poolSQLiteConnect;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly SQLiteexceptionDelegate _sQLiteexceptionDelegate;
        private readonly delegateException _delegateException;
        private readonly TaskCancelExceptionDelegate _taskCanceledException;
        private readonly IMemoryCache _memoryCache;

        public SelectWratherRequest(ILogger<SelectWratherRequest> logger, PoolSQLiteConnect poolSQLiteConnect, SemaphoreSlim semaphore,
            SQLiteexceptionDelegate sQLiteexceptionDelegate, delegateException delegateException, TaskCancelExceptionDelegate taskCanceledException, IMemoryCache memoryCache)
        {
            _logger = logger;
            _poolSQLiteConnect = poolSQLiteConnect;
            _semaphore = semaphore;
            _sQLiteexceptionDelegate = sQLiteexceptionDelegate;
            _delegateException = delegateException;
            _taskCanceledException = taskCanceledException;
            _memoryCache = memoryCache;
        }

        public async Task<List<RequestWeatherSelect>> CacheRequest(CancellationToken cancellation = default)
        {
            string cachekey = "key_cacheWeather";
            string stalecache = $"stale{cachekey}";
            List<RequestWeatherSelect> oldcache = null;

            if (_memoryCache.TryGetValue(cachekey, out List<RequestWeatherSelect> cached))
            {
                oldcache = cached;
                return cached;
            }
            await _semaphore.WaitAsync(cancellation);

            try
            {
                if (_memoryCache.TryGetValue(cachekey, out List<RequestWeatherSelect> cached2))
                {
                    return cached2;
                }

                var fallback = Policy<List<RequestWeatherSelect>>
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
                        if (_memoryCache.TryGetValue(stalecache, out List<RequestWeatherSelect> stalecached))
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

                var fallbackresult = await fallback.ExecuteAsync(async () =>
                {
                    var result = await Request(cancellation).ConfigureAwait(false);

                    if (result != null)
                    {
                        var options = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(15))
                        .SetSlidingExpiration(TimeSpan.FromMinutes(5));

                        _memoryCache.Set(cachekey, result, options);

                        var staleoptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(15));

                        _memoryCache.Set(stalecache, result, staleoptions);
                        _logger.LogInformation("✅ Cached fresh data for {CacheCode}", cachekey);
                        return result;
                    }
                    else
                    {
                        _logger.LogInformation("✅ Using cached data for {CacheCode}", cachekey);
                        return default;
                    }
                });
                return fallbackresult;
            }
            catch (Exception ex)
            {
                return await _delegateException.RunDelegate(_delegateException.Delegate<RequestWeatherSelect>, ex);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<RequestWeatherSelect>> Request(CancellationToken cancellation = default)
        {
            SQLiteConnection connection = null;
            SQLiteTransaction transaction = null;
            List<RequestWeatherSelect> listresult = null;
            try
            {
                connection = _poolSQLiteConnect.ConnectionOpen();

                await using (transaction = connection.BeginTransaction())
                {
                    string command = "SELECT Id, Timezone, Temperature, ApparentTemperature, RelativeHumidity, Precipitation, WeatherCode,  WindSpeed, WindDirection, DateUpdate FROM WeatherCurrent";

                    await using (var sqlcommand = new SQLiteCommand(command, connection, transaction))
                    {
                        var result = await sqlcommand.ExecuteReaderAsync().ConfigureAwait(false);

                        if (result != null)
                        {
                            while (await result.ReadAsync())
                            {
                                string Id = result.GetString(0);
                                string Timezone = result.GetString(1);
                                string Temperature = result.GetString(2);
                                string ApparentTemperature = result.GetString(3);
                                string RelativeHumidity = result.GetString(4);
                                string Precipitation = result.GetString(5);
                                string WeatherCode = result.GetString(6);
                                string WindSpeed = result.GetString(7);
                                string WindDirection = result.GetString(8);
                                string DateUpdate = result.GetString(9);

                                var resultat = new RequestWeatherSelect
                                {
                                    Id = Id,
                                    Timezone = Timezone,
                                    Temperature = Temperature,
                                    ApparentTemperature = ApparentTemperature,
                                    RelativeHumidity = RelativeHumidity,
                                    Precipitation = Precipitation,
                                    WeatherCode = WeatherCode,
                                    WindSpeed = WindSpeed,
                                    WindDirection = WindDirection,
                                    DateUpdate = DateUpdate,
                                };

                                listresult.Add(resultat);
                            }
                            return listresult;
                        }
                        else
                        {
                            _logger.LogError("Результат запроса погоды не найден");
                            return new List<RequestWeatherSelect>();
                        }
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                await (transaction?.RollbackAsync() ?? Task.CompletedTask);
                return await _taskCanceledException.RunDelegate(_taskCanceledException.ExceptionMethod<RequestWeatherSelect>, ex);
            }
            catch (SQLiteException ex)
            {
                await (transaction?.RollbackAsync() ?? Task.CompletedTask);
                return await _sQLiteexceptionDelegate.RunDelegate(_sQLiteexceptionDelegate.DelegateMethod<RequestWeatherSelect>, ex);
            }
            catch (Exception ex)
            {
                await (transaction?.RollbackAsync() ?? Task.CompletedTask);
                return await _delegateException.RunDelegate(_delegateException.Delegate<RequestWeatherSelect>, ex);
            }
            finally
            {
                if (transaction != null)
                {
                    transaction.Dispose();
                }
                if (connection != null)
                {
                    _poolSQLiteConnect.ConnectionClose(connection);
                }
            }
        }
    }
}
