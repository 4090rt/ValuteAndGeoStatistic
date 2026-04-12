using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValuteAndWeatherStatistic.ModelData;

namespace ValuteAndWeatherStatistic.DataBase
{
    public class InsertRequestCource
    {
        private readonly ILogger<InsertRequestCource> _logger;
        private readonly PoolSQLiteConnect _poolSQLiteConnect;
        private readonly SQLiteexceptionDelegate _sQLiteExceptionDelegate;
        public InsertRequestCource(ILogger<InsertRequestCource> logger, PoolSQLiteConnect poolSQLiteConnect, SQLiteexceptionDelegate sQLiteExceptionDelegate)
        {
            _logger = logger;
            _poolSQLiteConnect = poolSQLiteConnect;
            _sQLiteExceptionDelegate = sQLiteExceptionDelegate;
        }

        public async Task<bool> InserRequest(IAsyncEnumerable<List<CourceData>> list, DateTime date, CancellationToken cancellation = default)
        {
            if (list == null)
            {
                _logger.LogWarning("Пустой список, сохранение отменено");
                return false;
            }

            SQLiteConnection? connection = null;
            SQLiteTransaction? sQLiteTransaction = null;
            try
            {
                connection = _poolSQLiteConnect.ConnectionOpen();

                await using (sQLiteTransaction = connection.BeginTransaction())
                {
                    string command = "INSERT INTO [CurrencyRates] (Result, TimeLastUpdateUtc, TimeNextUpdateUtc, BaseCode, ConversionRates, DateUpdate) VALUES(@R, @TLUUTC, @TNUUTC, @B, @C, @D)";

                    await using (var sqlcommand = new SQLiteCommand(command, connection, sQLiteTransaction))
                    {
                        await foreach (var item in list)
                        {
                            var course = item.FirstOrDefault();
                            if (course == null) continue;

                            sqlcommand.Parameters.AddWithValue("@R", course.Result);
                            sqlcommand.Parameters.AddWithValue("@TLUUTC", course.TimeLastUpdateUtc);
                            sqlcommand.Parameters.AddWithValue("@TNUUTC", course.TimeNextUpdateUtc);
                            sqlcommand.Parameters.AddWithValue("@B", course.BaseCode);
                            sqlcommand.Parameters.AddWithValue("@C", string.Join(",", course.ConversionRates.Select(kv => $"{kv.Key}:{kv.Value}")));
                            sqlcommand.Parameters.AddWithValue("@D", date);

                            await sqlcommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }
                    }
                    await sQLiteTransaction.CommitAsync().ConfigureAwait(false);
                }
                return true;
            }
            catch (SQLiteException ex)
            {
                return await _sQLiteExceptionDelegate.RunDelegate(_sQLiteExceptionDelegate.DelegateMethod<CourceData>, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Возникло исключение");
                await (sQLiteTransaction?.RollbackAsync() ?? Task.CompletedTask).ConfigureAwait(false);
                return false;
            }
            finally
            {
                sQLiteTransaction.Dispose();
                if (connection != null)
                {
                    _poolSQLiteConnect.ConnectionClose(connection);
                }
            }
        }
    }
}
