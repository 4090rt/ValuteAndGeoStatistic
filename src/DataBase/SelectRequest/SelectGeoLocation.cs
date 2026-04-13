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
using System.Transactions;
using ValuteAndWeatherStatistic.DelegateException;
using ValuteAndWeatherStatistic.ModelData;
using ValuteAndWeatherStatistic.ModelData.ModelDataSelectRequest;

namespace ValuteAndWeatherStatistic.DataBase.SelectRequest
{
    public class SelectGeoLocation
    {
        private readonly ILogger<SelectGeoLocation> _logger;
        private readonly PoolSQLiteConnect _poolSQLiteConnect;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly SQLiteexceptionDelegate _sQLiteexceptionDelegate;
        private readonly delegateException _delegateException;
        private readonly TaskCancelExceptionDelegate _taskCanceledException;
        private readonly IMemoryCache _memoryCache;

        public SelectGeoLocation(ILogger<SelectGeoLocation> logger, PoolSQLiteConnect poolSQLiteConnect, SemaphoreSlim semaphore,
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

        public async Task<List<RequestGeoLocationSElect>> CacheRequest(CancellationToken cancellation = default)
        { 
            string cachekey = $"cachekeySelectGeoLoc";
            List<RequestGeoLocationSElect> oldcache = null;
            string stalekey = $"stale{cachekey}";

            if (_memoryCache.TryGetValue(cachekey, out List<RequestGeoLocationSElect> cached))
            { 
                oldcache = cached;
                return cached;
            }
            await _semaphore.WaitAsync(cancellation);
            try
            {
                if (_memoryCache.TryGetValue(cachekey, out List<RequestGeoLocationSElect> cached2))
                {
                    return cached2;
                }

                var fallback = Policy<List<RequestGeoLocationSElect>>
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
                        if (_memoryCache.TryGetValue(stalekey, out List<RequestGeoLocationSElect> stalecached))
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

                        _memoryCache.Set(stalekey, result, staleoptions);
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
                return await _delegateException.RunDelegate(_delegateException.Delegate<RequestGeoLocationSElect>, ex);
            }
            finally
            { 
                _semaphore.Release();
            }
        }

        public async Task<List<RequestGeoLocationSElect>> Request(CancellationToken cancellation = default)
        { 
            SQLiteConnection connection = null;
            SQLiteTransaction transaction = null;
            var resultlist = new List<RequestGeoLocationSElect>();
            try
            {
                connection = _poolSQLiteConnect.ConnectionOpen();

                using (transaction = connection.BeginTransaction())
                {
                    string command = "SELECT  ContinentName, CountryCode2, CountryName, CountryNameOfficial, " +
                        "StateProv, District, City,DateUpdate FROM GeoLocations";

                    using (var sqlcommand = new SQLiteCommand(command, connection, transaction))
                    { 
                       var result =  await sqlcommand.ExecuteReaderAsync().ConfigureAwait(false);
                        if (result != null)
                        {
                            while (await result.ReadAsync())
                            {
                                string ContinentName = result.GetString(0);
                                string CountryCode2 = result.GetString(1);
                                string CountryName = result.GetString(2);
                                string CountryNameOfficial = result.GetString(3);
                                string StateProv = result.GetString(4);
                                string District = result.GetString(5);
                                string City = result.GetString(6);
                                string DateUpdate = result.GetString(7);
                               
                                var res = new RequestGeoLocationSElect()
                                {
                                    ContinentName = ContinentName,
                                    CountryCode2 = CountryCode2,
                                    CountryName = CountryName,
                                    CountryNameOfficial = CountryNameOfficial,
                                    StateProv = StateProv,
                                    District = District,
                                    City = City,
                                    DateUpdate = DateUpdate,
                                };
                                resultlist.Add(res);
                            }
                            return resultlist;
                        }
                        else
                        {
                            _logger.LogError("Результат запроса геолокации не найден");
                            return new List<RequestGeoLocationSElect>();
                        }
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                await (transaction.RollbackAsync() ?? Task.CompletedTask).ConfigureAwait(false);
                return await _taskCanceledException.RunDelegate(_taskCanceledException.ExceptionMethod<RequestGeoLocationSElect>, ex);
            }
            catch (SQLiteException ex)
            {
                await (transaction.RollbackAsync() ?? Task.CompletedTask).ConfigureAwait(false);
                return await _sQLiteexceptionDelegate.RunDelegate(_sQLiteexceptionDelegate.DelegateMethod<RequestGeoLocationSElect>, ex);
            }
            catch (Exception ex)
            {
                await (transaction?.RollbackAsync() ?? Task.CompletedTask).ConfigureAwait(false);
                return await _delegateException.RunDelegate(_delegateException.Delegate<RequestGeoLocationSElect>, ex);
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
