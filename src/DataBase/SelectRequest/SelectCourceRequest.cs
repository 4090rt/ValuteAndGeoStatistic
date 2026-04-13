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
    public class SelectCourceRequest
    {
        private readonly ILogger<SelectCourceRequest> _logger;
        private readonly PoolSQLiteConnect _poolSQLiteConnect;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly SQLiteexceptionDelegate _sQLiteexceptionDelegate;
        private readonly delegateException _delegateException;
        private readonly TaskCancelExceptionDelegate _taskCanceledException;
        private readonly IMemoryCache _memoryCache;

        public SelectCourceRequest(ILogger<SelectCourceRequest> logger, PoolSQLiteConnect poolSQLiteConnect, SemaphoreSlim semaphore,
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

        public async Task<List<RequestCourceSelect>> CacheRequest(CancellationToken cancellation = default)
        {
            string cachekey = "cachekeySelectCource";
            string stalekey = $"stale{cachekey}";
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
                        var IsEmpty = outcome.Result == null;

                        if (exception != null)
                        {
                            _logger.LogWarning($"⚠️ Fallback by exception: {exception.Message}");
                        }
                        if (IsEmpty)
                        {
                            _logger.LogWarning($"⚠️ Fallback by empty result");
                        }
                        if (oldcache != null)
                        {
                            _logger.LogInformation("✅ Fallback: возвращаю старые данные из кэша");
                            return oldcache;
                        }
                        if (_memoryCache.TryGetValue(stalekey, out List<RequestCourceSelect> stalecached))
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
                return await _delegateException.RunDelegate(_delegateException.Delegate<RequestCourceSelect>, ex);
            }
            finally
            { 
                _semaphore.Release();
            }
        }

        public async Task<List<RequestCourceSelect>> Request(CancellationToken cancellation = default)
        {
            SQLiteConnection connection = null;
            SQLiteTransaction transaction = null;
            var resultlist = new List<RequestCourceSelect>();
            try
            {
                connection = _poolSQLiteConnect.ConnectionOpen();

                await using (transaction = connection.BeginTransaction())
                {
                    string command = "SELECT BaseCode, ConversionRates, DateUpdate FROM CurrencyRates";

                    await using (var commandsql = new SQLiteCommand(command, connection, transaction))
                    {
                        var result = await commandsql.ExecuteReaderAsync().ConfigureAwait(false);

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
                                resultlist.Add(resultreturn);
                            }
                            return resultlist;
                        }
                        else
                        {
                            _logger.LogError("Результат запроса валют не найден");
                            return new List<RequestCourceSelect>();
                        }
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                await (transaction.RollbackAsync() ?? Task.CompletedTask);
                return await _taskCanceledException.RunDelegate(_taskCanceledException.ExceptionMethod<RequestCourceSelect>, ex);
            }
            catch (SQLiteException ex)
            {
                await (transaction.RollbackAsync() ?? Task.CompletedTask);
                return await _sQLiteexceptionDelegate.RunDelegate(_sQLiteexceptionDelegate.DelegateMethod<RequestCourceSelect>, ex);
            }
            catch (Exception ex)
            {
                await (transaction.RollbackAsync() ?? Task.CompletedTask);
                return await _delegateException.RunDelegate(_delegateException.Delegate<RequestCourceSelect>, ex);
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
