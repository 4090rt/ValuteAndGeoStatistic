using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValuteAndWeatherStatistic.DelegateException;
using ValuteAndWeatherStatistic.ModelData;

namespace ValuteAndWeatherStatistic.DataBase.InsertRequest
{
    public class InsertReqeustWeather
    {
        private readonly ILogger<InsertReqeustWeather> _logger;
        private readonly PoolSQLiteConnect _poolSQLiteConnect;
        private readonly SQLiteexceptionDelegate _sQLiteExceptionDelegate;
        public InsertReqeustWeather(ILogger<InsertReqeustWeather> logger, PoolSQLiteConnect poolSQLiteConnect, SQLiteexceptionDelegate sQLiteExceptionDelegate)
        {
            _logger = logger;
            _poolSQLiteConnect = poolSQLiteConnect;
            _sQLiteExceptionDelegate = sQLiteExceptionDelegate;
        }

        public async Task<bool> WeatherRequest(IAsyncEnumerable<List<WeatherData>> list, DateTime date, CancellationToken cancellation = default)
        {
            SQLiteConnection connection = null;
            SQLiteTransaction transaction = null;
            try
            {
                connection = _poolSQLiteConnect.ConnectionOpen();

                await using (transaction = connection.BeginTransaction())
                {
                    string command = "INSERT INTO WeatherCurrent (" +
                        "CoordinatesId, Latitude, Longitude, GenerationTimeMs, UtcOffsetSeconds, " +
                        "Timezone, TimezoneAbbreviation, Time, Interval, " +
                        "Temperature, ApparentTemperature, RelativeHumidity, Precipitation, " +
                        "WeatherCode, WindSpeed, WindDirection, DateUpdate" +
                        ") VALUES (" +
                        "@CoordinatesId, @Latitude, @Longitude, @GenerationTimeMs, @UtcOffsetSeconds, " +
                        "@Timezone, @TimezoneAbbreviation, @Time, @Interval, " +
                        "@Temperature, @ApparentTemperature, @RelativeHumidity, @Precipitation, " +
                        "@WeatherCode, @WindSpeed, @WindDirection, @DateUpdate" +
                        ")";

                    await using (var SQLCOMMAND = new SQLiteCommand(command, connection, transaction))
                    {
                        await foreach (var item in list)
                        {
                            var weather = item.FirstOrDefault();
                            if (weather == null) continue;

                            SQLCOMMAND.Parameters.Clear();
                            SQLCOMMAND.Parameters.AddWithValue("@CoordinatesId", DBNull.Value);
                            SQLCOMMAND.Parameters.AddWithValue("@Latitude", weather.Latitude);
                            SQLCOMMAND.Parameters.AddWithValue("@Longitude", weather.Longitude);
                            SQLCOMMAND.Parameters.AddWithValue("@GenerationTimeMs", weather.GenerationTimeMs);
                            SQLCOMMAND.Parameters.AddWithValue("@UtcOffsetSeconds", weather.UtcOffsetSeconds);
                            SQLCOMMAND.Parameters.AddWithValue("@Timezone", (object?)weather.Timezone ?? DBNull.Value);
                            SQLCOMMAND.Parameters.AddWithValue("@TimezoneAbbreviation", (object?)weather.TimezoneAbbreviation ?? DBNull.Value);
                            SQLCOMMAND.Parameters.AddWithValue("@Time", (object?)weather.Current.Time ?? DBNull.Value);
                            SQLCOMMAND.Parameters.AddWithValue("@Interval", weather.Current.Interval);
                            SQLCOMMAND.Parameters.AddWithValue("@Temperature", weather.Current.Temperature2m);
                            SQLCOMMAND.Parameters.AddWithValue("@ApparentTemperature", weather.Current.ApparentTemperature);
                            SQLCOMMAND.Parameters.AddWithValue("@RelativeHumidity", weather.Current.RelativeHumidity2m);
                            SQLCOMMAND.Parameters.AddWithValue("@Precipitation", weather.Current.Precipitation);
                            SQLCOMMAND.Parameters.AddWithValue("@WeatherCode", weather.Current.WeatherCode);
                            SQLCOMMAND.Parameters.AddWithValue("@WindSpeed", weather.Current.WindSpeed10m);
                            SQLCOMMAND.Parameters.AddWithValue("@WindDirection", weather.Current.WindDirection10m);
                            SQLCOMMAND.Parameters.AddWithValue("@DateUpdate", date.ToString("yyyy-MM-dd HH:mm:ss"));

                            await SQLCOMMAND.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }
                    }
                    await transaction.CommitAsync().ConfigureAwait(false);
                }
                return true;
            }
            catch (SQLiteException ex)
            {
                await _sQLiteExceptionDelegate.RunDelegate(_sQLiteExceptionDelegate.DelegateMethod<WeatherData>, ex);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Возникло исключение при работе");
                await (transaction?.RollbackAsync() ?? Task.CompletedTask).ConfigureAwait(false);
                return false;
            }
            finally
            {
                transaction.Dispose();
                if (connection != null)
                {
                    _poolSQLiteConnect.ConnectionClose(connection);
                }
            }
        }
    }
}
