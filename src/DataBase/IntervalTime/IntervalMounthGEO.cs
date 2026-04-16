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

namespace ValuteAndWeatherStatistic.DataBase.IntervalTime
{
    public class IntervalMounthGEO
    {
        private readonly ILogger<IntervalMounthGEO> _logger;
        private readonly PoolSQLiteConnect _poolSQLiteConnect;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly SQLiteexceptionDelegate _sQLiteexceptionDelegate;
        private readonly delegateException _delegateException;
        private bool _ischeked = false;
        private readonly TaskCancelExceptionDelegate _taskCanceledException;
        private readonly IMemoryCache _memoryCache;

        public IntervalMounthGEO(ILogger<IntervalMounthGEO> logger, PoolSQLiteConnect poolSQLiteConnect, SemaphoreSlim semaphore,
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

        public async Task Inithializate()
        {
            if (_ischeked == true) return;

            if (_ischeked == false)
            {
                await CreateIdex();
                await IndexProverka();
            }

            _ischeked = true;
        }

        public async Task<List<RequestGeoLocationSElect>> CacheReques(int page, int pagecount, CancellationToken cancellation = default)
        {
            string cachekey = "key_cacheGEOMOUNTH";
            string stalecache = $"stale{cachekey}";
            List<RequestGeoLocationSElect> oldcache = null;

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
                        if (_memoryCache.TryGetValue(stalecache, out List<RequestGeoLocationSElect> stalecached))
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
                    var result = await Request(page, pagecount, cancellation).ConfigureAwait(false);

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
                return await _delegateException.RunDelegate(_delegateException.Delegate<RequestGeoLocationSElect>, ex);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<RequestGeoLocationSElect>> Request(int page, int pagecount, CancellationToken cancellation = default)
        {
            int offSet = (page - 1) * pagecount;
            SQLiteConnection connection = null;
            List<RequestGeoLocationSElect> listresult = new List<RequestGeoLocationSElect>();
            try
            {
                connection = _poolSQLiteConnect.ConnectionOpen();
                string command = "SELECT * FROM GeoLocations WHERE date(DateUpdate) = date('now', '-30 days') ORDER BY DateUpdate DESC LIMIT @limit OFFSET @offset";

                await using (var commandsql = new SQLiteCommand(command, connection))
                {
                    commandsql.Parameters.AddWithValue("@limit", pagecount);
                    commandsql.Parameters.AddWithValue("@offset", offSet);

                    using (var result = await commandsql.ExecuteReaderAsync())
                    {
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
                                listresult.Add(res);
                            }
                            return listresult;
                        }
                        else
                        {
                            _logger.LogError("Результат запроса геолокации за 30 дней не найден");
                            return default;
                        }
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                return await _taskCanceledException.RunDelegate(_taskCanceledException.ExceptionMethod<RequestGeoLocationSElect>, ex);
            }
            catch (SQLiteException ex)
            {
                return await _sQLiteexceptionDelegate.RunDelegate(_sQLiteexceptionDelegate.DelegateMethod<RequestGeoLocationSElect>, ex);
            }
            catch (Exception ex)
            {
                return await _delegateException.RunDelegate(_delegateException.Delegate<RequestGeoLocationSElect>, ex);
            }
            finally
            {
                if (connection != null)
                {
                    _poolSQLiteConnect.ConnectionClose(connection);
                }
            }
        }

        public async Task CreateIdex()
        {
            SQLiteConnection connection = null;
            try
            {
                connection = _poolSQLiteConnect.ConnectionOpen();

                string command = "CREATE INDEX IF NOT EXISTS IX_DayGEO_DateMounth ON GeoLocations(DateUpdate)";

                await using (var sqlcommand = new SQLiteCommand(command, connection))
                {
                    await sqlcommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                    _logger.LogInformation("Индекс для DateUpdate  GeoLocations создан");
                }
            }
            catch (TaskCanceledException ex)
            {
                await _taskCanceledException.RunDelegate(_taskCanceledException.ExceptionMethod<RequestGeoLocationSElect>, ex);
            }
            catch (SQLiteException ex)
            {
                await _sQLiteexceptionDelegate.RunDelegate(_sQLiteexceptionDelegate.DelegateMethod<RequestGeoLocationSElect>, ex);
            }
            catch (Exception ex)
            {
                await _delegateException.RunDelegate(_delegateException.Delegate<RequestGeoLocationSElect>, ex);
            }
            finally
            {
                if (connection != null)
                {
                    _poolSQLiteConnect.ConnectionClose(connection);
                }
            }
        }

        public async Task<bool> IndexProverka()
        {
            SQLiteConnection connection = null;
            try
            {
                connection = _poolSQLiteConnect.ConnectionOpen();

                string command = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = 'IX_DayGEO_DateMounth' AND tbl_name = 'GeoLocations'";

                await using (var commandsql = new SQLiteCommand(command, connection))
                {
                    var resule = await commandsql.ExecuteScalarAsync().ConfigureAwait(false);
                    if (resule != null && resule != DBNull.Value)
                    {
                        bool exec = Convert.ToInt32(resule) == 1;

                        if (exec)
                        {
                            _logger.LogInformation($"✅ Индекс 'IX_DayGEO_DateMounth' существует!");
                        }
                        else
                        {
                            _logger.LogInformation($"❌ Индекс 'IX_DayGEO_DateMounth' не найден");
                        }
                        return exec;
                    }
                    return false;
                }
            }
            catch (TaskCanceledException ex)
            {
                await _taskCanceledException.RunDelegate(_taskCanceledException.ExceptionMethod<RequestGeoLocationSElect>, ex);
                return false;
            }
            catch (SQLiteException ex)
            {
                await _sQLiteexceptionDelegate.RunDelegate(_sQLiteexceptionDelegate.DelegateMethod<RequestGeoLocationSElect>, ex);
                return false;
            }
            catch (Exception ex)
            {
                await _delegateException.RunDelegate(_delegateException.Delegate<RequestGeoLocationSElect>, ex);
                return false;
            }
            finally
            {
                if (connection != null)
                {
                    _poolSQLiteConnect.ConnectionClose(connection);
                }
            }
        }
    }
}
