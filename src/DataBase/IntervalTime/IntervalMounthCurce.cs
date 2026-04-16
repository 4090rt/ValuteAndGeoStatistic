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
    public class IntervalMounthCurce
    {
        private readonly ILogger<IntervalMounthCurce> _logger;
        private readonly PoolSQLiteConnect _poolSQLiteConnect;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly SQLiteexceptionDelegate _sQLiteexceptionDelegate;
        private readonly delegateException _delegateException;
        private bool _ischeked = false;
        private readonly TaskCancelExceptionDelegate _taskCanceledException;
        private readonly IMemoryCache _memoryCache;

        public IntervalMounthCurce(ILogger<IntervalMounthCurce> logger, PoolSQLiteConnect poolSQLiteConnect, SemaphoreSlim semaphore,
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

        public async Task Inthializate()
        {
            if (_ischeked == true) return;

            if (_ischeked == false)
            {
                await CreateIdex();
                await IndexProverka();
            }

            _ischeked = true;
        }

        public async Task<List<RequestCourceSelect>> CacheRequest(int pagecount, int page, CancellationToken cancellation = default)
        {
            string cache_key = "cache_keyCourceMounth";
            string stalecache = $"stale{cache_key}";
            List<RequestCourceSelect> oldcache = new List<RequestCourceSelect>();
            
            if (_memoryCache.TryGetValue(cache_key, out List<RequestCourceSelect> cached))
            {
                oldcache = cached;
                return cached;
            }

            await _semaphore.WaitAsync(cancellation);
            try
            {
                if (_memoryCache.TryGetValue(cache_key, out List<RequestCourceSelect> cached2))
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
                        var isEty = outcome.Result == null;

                        if (exception != null)
                        {
                            _logger.LogWarning($"⚠️ Fallback by exception: {exception.Message}");
                        }
                        if (isEty)
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
                    },onFallbackAsync: async (outcome, ctx) =>
                    {
                        await Task.CompletedTask;
                        _logger.LogError($"🆘 Fallback сработал: {outcome.Exception?.Message}");
                    });

                var fallbackresult = await fallback.ExecuteAsync(async () =>
                {
                    var result = await Request(pagecount, page, cancellation).ConfigureAwait(false);

                    if (result != null)
                    {
                        var options = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(15))
                        .SetSlidingExpiration(TimeSpan.FromMinutes(5));

                        _memoryCache.Set(cache_key, result, options);

                        var staleoptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(15));

                        _memoryCache.Set(stalecache, result, staleoptions);
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
                return await _delegateException.RunDelegate(_delegateException.Delegate<RequestCourceSelect>, ex);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<RequestCourceSelect>> Request(int pagecount, int page, CancellationToken cancellation = default)
        {
            int offset = (page - 1) * pagecount;
            SQLiteConnection connection = null;
            List<RequestCourceSelect> listresult = new List<RequestCourceSelect>();
            try
            {
                connection = _poolSQLiteConnect.ConnectionOpen();

                string command = "SELECT * FROM CurrencyRates WHERE DateUpdate = date('now', '-30 days')ORDER BY DateUpdate DESC LIMIT @lIMIT OFFSET @Offset";

                using (var sqlcommand = new SQLiteCommand(command, connection))
                { 
                    var result = await sqlcommand.ExecuteReaderAsync().ConfigureAwait(false);

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
                        _logger.LogError("Результат запроса курса валют за месяц не найден");
                        return default;
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                return await _taskCanceledException.RunDelegate(_delegateException.Delegate<RequestCourceSelect>, ex);
            }
            catch (SQLiteException ex)
            {
                return await _sQLiteexceptionDelegate.RunDelegate(_delegateException.Delegate<RequestCourceSelect>,ex);
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
                string command = "CREATE INDEX IF NOT EXISTS IX_CurrencyRatesMounth ON CurrencyRates(DateUpdate)";

                await using (var sqlcommand = new SQLiteCommand(command, connection))
                { 
                    await sqlcommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                    _logger.LogInformation("Индекс для DateUpdate_Mounth  CurrencyRates создан");
                }
            }
            catch (TaskCanceledException ex)
            {
                await _taskCanceledException.RunDelegate(_delegateException.Delegate<RequestCourceSelect>, ex);
            }
            catch (SQLiteException ex)
            {
                await _sQLiteexceptionDelegate.RunDelegate(_delegateException.Delegate<RequestCourceSelect>, ex);
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
                string command = "SELECET COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = 'IX_CurrencyRatesMounth' AND tbl_name = 'DateUpdate'";

                using (var sqlcommand = new SQLiteCommand(command, connection))
                { 
                    var result = await sqlcommand.ExecuteScalarAsync().ConfigureAwait(false);

                    if (result != null && result != DBNull.Value)
                    {
                        bool exec = Convert.ToInt32(result) == 1;

                        if (exec)
                        {
                            _logger.LogInformation($"✅ Индекс 'IX_CurrencyRatesMounth' существует!");
                        }
                        else
                        {
                            _logger.LogInformation($"✅ Индекс 'IX_CurrencyRatesMounth' не найден!");
                        }
                    }
                    return false;
                }
            }
            catch (TaskCanceledException ex)
            {
                await _taskCanceledException.RunDelegate(_delegateException.Delegate<RequestCourceSelect>, ex);
                return false;
            }
            catch (SQLiteException ex)
            {
                await _sQLiteexceptionDelegate.RunDelegate(_delegateException.Delegate<RequestCourceSelect>, ex);
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
