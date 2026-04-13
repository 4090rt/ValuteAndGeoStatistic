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
    public class InsertRequestGeoLoc
    {
        private readonly ILogger<InsertRequestGeoLoc> _logger;
        private readonly PoolSQLiteConnect _poolSQLiteConnect;
        private readonly SQLiteexceptionDelegate _sQLiteExceptionDelegate;

        public InsertRequestGeoLoc(ILogger<InsertRequestGeoLoc> logger, PoolSQLiteConnect poolSQLiteConnect, SQLiteexceptionDelegate sQLiteExceptionDelegate)
        {
            _logger = logger;
            _poolSQLiteConnect = poolSQLiteConnect;
            _sQLiteExceptionDelegate = sQLiteExceptionDelegate;
        }

        public async Task<bool> RequestGeolLoc(IAsyncEnumerable<List<Geo>> list, DateTime date, CancellationToken cancellation = default)
        {
            if (list == null)
            {
                _logger.LogWarning("Пустой список, сохранение геолокации отменено");
                return false;
            }

            SQLiteConnection connection = null;
            SQLiteTransaction transaction = null;

            try
            {
                connection = _poolSQLiteConnect.ConnectionOpen();

                await using (transaction = connection.BeginTransaction())
                {
                    string command = @"INSERT INTO [GeoLocations]
                        (ContinentCode, ContinentName, CountryCode2, CountryCode3, CountryName, CountryNameOfficial, IsEu,
                         StateProv, StateCode, District, City, Zipcode, Latitude, Longitude, DateUpdate)
                        VALUES
                        (@ContinentCode, @ContinentName, @CountryCode2, @CountryCode3, @CountryName, @CountryNameOfficial, @IsEu,
                         @StateProv, @StateCode, @District, @City, @Zipcode, @Latitude, @Longitude, @DateUpdate)";

                    await using (var sqlcommand = new SQLiteCommand(command, connection, transaction))
                    {
                        await foreach (var item in list)
                        {
                            var geo = item.FirstOrDefault();
                            if (geo == null) continue;

                            sqlcommand.Parameters.Clear();
                            sqlcommand.Parameters.AddWithValue("@ContinentCode", (object?)geo.ContinentCode ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@ContinentName", (object?)geo.ContinentName ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@CountryCode2", (object?)geo.CountryCode2 ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@CountryCode3", (object?)geo.CountryCode3 ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@CountryName", (object?)geo.CountryName ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@CountryNameOfficial", (object?)geo.CountryNameOfficial ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@IsEu", Convert.ToInt32(geo.IsEu));
                            sqlcommand.Parameters.AddWithValue("@StateProv", (object?)geo.StateProv ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@StateCode", (object?)geo.StateCode ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@District", (object?)geo.District ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@City", (object?)geo.City ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@Zipcode", (object?)geo.Zipcode ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@Latitude", (object?)geo.Latitude ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@Longitude", (object?)geo.Longitude ?? DBNull.Value);
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
                _logger.LogError(ex, "SQLite ошибка при сохранении геолокации");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Возникло исключение при сохранении геолокации");
                await (transaction?.RollbackAsync() ?? Task.CompletedTask).ConfigureAwait(false);
                return false;
            }
            finally
            {
                transaction?.Dispose();
                connection?.Close();
                _poolSQLiteConnect.ConnectionClose(connection);
            }
        }
    }
}
