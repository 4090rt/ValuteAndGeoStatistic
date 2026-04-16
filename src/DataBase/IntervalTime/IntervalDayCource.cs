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
using ValuteAndWeatherStatistic.DataBase.SelectRequest;
using ValuteAndWeatherStatistic.DelegateException;
using ValuteAndWeatherStatistic.ModelData.ModelDataSelectRequest;
using ValuteAndWeatherStatistic.ModelData.Parametrs;

namespace ValuteAndWeatherStatistic.DataBase.IntervalTime
{
    public class IntervalDayCource
    {
        private readonly ILogger<IntervalDayCource> _logger;
        private readonly PoolSQLiteConnect _poolSQLiteConnect;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly SQLiteexceptionDelegate _sQLiteexceptionDelegate;
        private readonly delegateException _delegateException;
        private bool _ischeked = false;
        private readonly TaskCancelExceptionDelegate _taskCanceledException;
        private readonly IMemoryCache _memoryCache;

        public IntervalDayCource(ILogger<IntervalDayCource> logger, PoolSQLiteConnect poolSQLiteConnect, SemaphoreSlim semaphore,
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

        public async Task<List<RequestCourceSelect>> CacheReques(int page, int pagecount, CancellationToken cancellation = default)
        {
            string cachekey = "key_cacheCource";
            string stalecache = $"stale{cachekey}";
            List<RequestCourceSelect> oldcache = null;

            if (_memoryCache.TryGetValue(cachekey, out List<RequestCourceSelect> cached))
            {
                oldcache = cached;
                return cached;
            }
            await _semaphore.WaitAsync(cancellation);

            try
            {
                if (_memoryCache.TryGetValue(cachekey, out List<RequestCourceSelect> cached2))
                {
                    return cached2;
                }

                var fallback = Policy<List<RequestCourceSelect>>
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
                        if (_memoryCache.TryGetValue(stalecache, out List<RequestCourceSelect> stalecached))
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
                return await _delegateException.RunDelegate(_delegateException.Delegate<RequestCourceSelect>, ex);
            }
            finally
            {
                _semaphore.Release();
             }
        }

        public async Task<List<RequestCourceSelect>> Request(int page, int pagecount,CancellationToken cancellation = default)
        {
            int offSet = (page - 1) * pagecount;
            SQLiteConnection connection = null;
            List<RequestCourceSelect> listresult = new List<RequestCourceSelect>();
            try
            {
                connection = _poolSQLiteConnect.ConnectionOpen();
                string command = "SELECT * FROM CurrencyRates WHERE date(DateUpdate) = date('now') ORDER BY DateUpdate DESC LIMIT @limit OFFSET @offset";

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
                                var BaseCode = result.GetString(0);
                                var ConversionRates = result.GetString(1);
                                var DateUpdate = result.GetString(2);

                                var resultreturn = new RequestCourceSelect()
                                {
                                    BaseCode = BaseCode,
                                    ConversionRates = ConversionRates,
                                    DateUpdate = DateUpdate
                                };
                                listresult.Add(resultreturn);
                            }
                            return listresult;
                        }
                        else
                        {
                            _logger.LogError("Результат запроса курса валют за день не найден");
                            return default;
                        }
                        
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                return await _taskCanceledException.RunDelegate(_taskCanceledException.ExceptionMethod<RequestCourceSelect>, ex);
            }
            catch (SQLiteException ex)
            {
                return await _sQLiteexceptionDelegate.RunDelegate(_sQLiteexceptionDelegate.DelegateMethod<RequestCourceSelect>, ex);
            }
            catch (Exception ex)
            {
                return await _delegateException.RunDelegate(_delegateException.Delegate<RequestCourceSelect>, ex);
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

                string command = "CREATE INDEX IF NOT EXISTS IX_CurrencyCource_Date ON CurrencyRates(DateUpdate)";

                await using (var sqlcommand = new SQLiteCommand(command, connection))
                { 
                    await sqlcommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                    _logger.LogInformation("Индекс для DateUpdate  CurrencyRates создан");
                }
            }
            catch (TaskCanceledException ex)
            {
                 await _taskCanceledException.RunDelegate(_taskCanceledException.ExceptionMethod<RequestCourceSelect>, ex);
            }
            catch (SQLiteException ex)
            {
                 await _sQLiteexceptionDelegate.RunDelegate(_sQLiteexceptionDelegate.DelegateMethod<RequestCourceSelect>, ex);
            }
            catch (Exception ex)
            {
                 await _delegateException.RunDelegate(_delegateException.Delegate<RequestCourceSelect>, ex);
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

                string command = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = 'IX_CurrencyCource_Date' AND tbl_name = 'CurrencyRates'";

                await using (var commandsql = new SQLiteCommand(command, connection))
                { 
                    var resule = await commandsql.ExecuteScalarAsync().ConfigureAwait(false);
                    if (resule != null && resule != DBNull.Value)
                    {
                        bool exec = Convert.ToInt32(resule) == 1;

                        if (exec)
                        {
                            _logger.LogInformation($"✅ Индекс 'IX_CurrencyCource_Date' существует!");
                        }
                        else
                        {
                            _logger.LogInformation($"❌ Индекс 'IX_CurrencyCource_Date' не найден");
                        }
                        return exec;
                    }
                    return false;
                }
            }
            catch (TaskCanceledException ex)
            {
                await _taskCanceledException.RunDelegate(_taskCanceledException.ExceptionMethod<RequestCourceSelect>, ex);
                return false;
            }
            catch (SQLiteException ex)
            {
                await _sQLiteexceptionDelegate.RunDelegate(_sQLiteexceptionDelegate.DelegateMethod<RequestCourceSelect>, ex);
                return false;
            }
            catch (Exception ex)
            {
                await _delegateException.RunDelegate(_delegateException.Delegate<RequestCourceSelect>, ex);
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
