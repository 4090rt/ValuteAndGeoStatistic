using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using ValuteAndWeatherStatistic.DelegateException;
using ValuteAndWeatherStatistic.ModelData;

namespace ValuteAndWeatherStatistic.DataBase.InsertRequest
{
    public class InsertRequestCordinats
    {
        private readonly ILogger<InsertRequestCordinats> _logger;
        private readonly PoolSQLiteConnect _poolSQLiteConnect;
        private readonly SQLiteexceptionDelegate _sQLiteExceptionDelegate;

        public InsertRequestCordinats(ILogger<InsertRequestCordinats> logger, PoolSQLiteConnect poolSQLiteConnect, SQLiteexceptionDelegate sQLiteExceptionDelegate)
        {
            _logger = logger;
            _poolSQLiteConnect = poolSQLiteConnect;
            _sQLiteExceptionDelegate = sQLiteExceptionDelegate;
        }

        public async Task<bool> CordinatsRequest(IAsyncEnumerable<List<GeoLocation>> list, DateTime date, CancellationToken cancellation = default)
        {
            if (list == null)
            {
                _logger.LogWarning("Пустой список, сохранение отменено");
                return false;
            }

            SQLiteConnection connection = null;
            SQLiteTransaction transaction = null;

            try
            {
                connection = _poolSQLiteConnect.ConnectionOpen();

                await using (transaction = connection.BeginTransaction())
                {
                    string command = @"INSERT INTO [Coordinates]
                        (Timezone, TimezoneOffset, TimezoneOffsetWithDst, Date, DateTime, DateTimeTxt, DateTimeWti, DateTimeYmd, DateTimeUnix,
                         Time24, Time12, Week, Month, Year, YearAbbr, CurrentTzAbbreviation, CurrentTzFullName,
                         StandardTzAbbreviation, StandardTzFullName, IsDst, DstSavings, DstExists,
                         DstTzAbbreviation, DstTzFullName, DstStart, DstEnd, DateUpdate)
                        VALUES
                        (@Timezone, @TimezoneOffset, @TimezoneOffsetWithDst, @Date, @DateTime, @DateTimeTxt, @DateTimeWti, @DateTimeYmd, @DateTimeUnix,
                         @Time24, @Time12, @Week, @Month, @Year, @YearAbbr, @CurrentTzAbbreviation, @CurrentTzFullName,
                         @StandardTzAbbreviation, @StandardTzFullName, @IsDst, @DstSavings, @DstExists,
                         @DstTzAbbreviation, @DstTzFullName, @DstStart, @DstEnd, @DateUpdate)";

                    await using (var sqlcommand = new SQLiteCommand(command, connection, transaction))
                    {
                        await foreach (var item in list)
                        {
                            var loc = item.FirstOrDefault();
                            if (loc == null) continue;

                            sqlcommand.Parameters.Clear();
                            sqlcommand.Parameters.AddWithValue("@Timezone", (object?)loc.Timezone ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@TimezoneOffset", loc.TimezoneOffset);
                            sqlcommand.Parameters.AddWithValue("@TimezoneOffsetWithDst", loc.TimezoneOffsetWithDst);
                            sqlcommand.Parameters.AddWithValue("@Date", (object?)loc.Date ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@DateTime", (object?)loc.DateTime ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@DateTimeTxt", (object?)loc.DateTimeTxt ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@DateTimeWti", (object?)loc.DateTimeWti ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@DateTimeYmd", (object?)loc.DateTimeYmd ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@DateTimeUnix", loc.DateTimeUnix);
                            sqlcommand.Parameters.AddWithValue("@Time24", (object?)loc.Time24 ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@Time12", (object?)loc.Time12 ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@Week", loc.Week);
                            sqlcommand.Parameters.AddWithValue("@Month", loc.Month);
                            sqlcommand.Parameters.AddWithValue("@Year", loc.Year);
                            sqlcommand.Parameters.AddWithValue("@YearAbbr", (object?)loc.YearAbbr ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@CurrentTzAbbreviation", (object?)loc.CurrentTzAbbreviation ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@CurrentTzFullName", (object?)loc.CurrentTzFullName ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@StandardTzAbbreviation", (object?)loc.StandardTzAbbreviation ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@StandardTzFullName", (object?)loc.StandardTzFullName ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@IsDst", Convert.ToInt32(loc.IsDst));
                            sqlcommand.Parameters.AddWithValue("@DstSavings", loc.DstSavings);
                            sqlcommand.Parameters.AddWithValue("@DstExists", Convert.ToInt32(loc.DstExists));
                            sqlcommand.Parameters.AddWithValue("@DstTzAbbreviation", (object?)loc.DstTzAbbreviation ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@DstTzFullName", (object?)loc.DstTzFullName ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@DstStart", (object?)loc.DstStart ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@DstEnd", (object?)loc.DstEnd ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@DateUpdate", date);

                            await sqlcommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }
                    }
                    await transaction.CommitAsync().ConfigureAwait(false);
                }
                return true;
            }
            catch (SQLiteException ex)
            {
                _sQLiteExceptionDelegate.RunDelegate(_sQLiteExceptionDelegate.DelegateMethod<GeoLocation>, ex).Wait();
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Возникло исключение");
                await (transaction?.RollbackAsync() ?? Task.CompletedTask);
                return false;
            }
            finally
            {
                transaction?.Dispose();
                if (connection != null)
                {
                    _poolSQLiteConnect.ConnectionClose(connection);
                }
            }
        }
    }
}
