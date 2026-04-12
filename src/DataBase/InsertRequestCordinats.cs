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


        public async Task<bool> CordinatsRequest(IAsyncEnumerable<List<CordinatsData>> list, DateTime date,CancellationToken cancellation = default)
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
                        ([Date], [Timezone], [TimezoneOffset], [TimezoneOffsetWithDst],
                         [DateTimeTxt], [DateTimeWti], [DateTimeYmd], [DateTimeUnix],
                         [Time24], [Time12], [Week], [Month], [Year], [YearAbbr],
                         [CurrentTzAbbreviation], [CurrentTzFullName], [StandardTzAbbreviation],
                         [StandardTzFullName], [IsDst], [DstSavings], [DstExists],
                         [DstTzAbbreviation], [DstTzFullName], [DstStart], [DstEnd],
                         [GeoLocation], [GeoCountry], [GeoState], [GeoCity], [GeoLocality], [GeoLatitude], [GeoLongitude])
                        VALUES
                        (@Date, @Timezone, @TimezoneOffset, @TimezoneOffsetWithDst,
                         @DateTimeTxt, @DateTimeWti, @DateTimeYmd, @DateTimeUnix,
                         @Time24, @Time12, @Week, @Month, @Year, @YearAbbr,
                         @CurrentTzAbbreviation, @CurrentTzFullName, @StandardTzAbbreviation,
                         @StandardTzFullName, @IsDst, @DstSavings, @DstExists,
                         @DstTzAbbreviation, @DstTzFullName, @DstStart, @DstEnd,
                         @GeoLocation, @GeoCountry, @GeoState, @GeoCity, @GeoLocality, @GeoLatitude, @GeoLongitude)";

                    await using (var sqlcommand = new SQLiteCommand(command, connection, transaction))
                    {
                        await foreach (var item in list)
                        {
                            var cordinat = item.FirstOrDefault();
                            if (cordinat == null) continue;

                            sqlcommand.Parameters.Clear();
                            sqlcommand.Parameters.AddWithValue("@Date", cordinat.Date ?? (object)DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@Timezone", cordinat.Timezone ?? (object)DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@TimezoneOffset", cordinat.TimezoneOffset);
                            sqlcommand.Parameters.AddWithValue("@TimezoneOffsetWithDst", cordinat.TimezoneOffsetWithDst);
                            sqlcommand.Parameters.AddWithValue("@DateTimeTxt", cordinat.DateTimeTxt ?? (object)DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@DateTimeWti", cordinat.DateTimeWti ?? (object)DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@DateTimeYmd", cordinat.DateTimeYmd ?? (object)DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@DateTimeUnix", cordinat.DateTimeUnix);
                            sqlcommand.Parameters.AddWithValue("@Time24", cordinat.Time24 ?? (object)DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@Time12", cordinat.Time12 ?? (object)DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@Week", cordinat.Week);
                            sqlcommand.Parameters.AddWithValue("@Month", cordinat.Month);
                            sqlcommand.Parameters.AddWithValue("@Year", cordinat.Year);
                            sqlcommand.Parameters.AddWithValue("@YearAbbr", cordinat.YearAbbr ?? (object)DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@CurrentTzAbbreviation", cordinat.CurrentTzAbbreviation ?? (object)DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@CurrentTzFullName", cordinat.CurrentTzFullName ?? (object)DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@StandardTzAbbreviation", cordinat.StandardTzAbbreviation ?? (object)DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@StandardTzFullName", cordinat.StandardTzFullName ?? (object)DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@IsDst", cordinat.IsDst);
                            sqlcommand.Parameters.AddWithValue("@DstSavings", cordinat.DstSavings);
                            sqlcommand.Parameters.AddWithValue("@DstExists", cordinat.DstExists);
                            sqlcommand.Parameters.AddWithValue("@DstTzAbbreviation", cordinat.DstTzAbbreviation ?? (object)DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@DstTzFullName", cordinat.DstTzFullName ?? (object)DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@DstStart", cordinat.DstStart ?? (object)DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@DstEnd", cordinat.DstEnd ?? (object)DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@GeoLocation", cordinat.Geo?.Location ?? (object)DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@GeoCountry", cordinat.Geo?.Country ?? (object)DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@GeoState", cordinat.Geo?.State ?? (object)DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@GeoCity", cordinat.Geo?.City ?? (object)DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@GeoLocality", cordinat.Geo?.Locality ?? (object)DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@GeoLatitude", cordinat.Geo?.Latitude ?? (object)DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@GeoLongitude", cordinat.Geo?.Longitude ?? (object)DBNull.Value);

                            await sqlcommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }
                    }
                    await transaction.CommitAsync().ConfigureAwait(false);
                }
                return true;
            }
            catch (SQLiteException ex)
            {
                return await _sQLiteExceptionDelegate.RunDelegate(_sQLiteExceptionDelegate.DelegateMethod<CordinatsData>, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Возникло исключение");
                await (transaction?.RollbackAsync() ?? Task.CompletedTask);
                return false;
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
