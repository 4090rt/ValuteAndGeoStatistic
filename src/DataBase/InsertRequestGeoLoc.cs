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

        public async Task<bool> RequestGeolLoc(IAsyncEnumerable<List<GeoLocation>> list, DateTime date, CancellationToken cancellation = default)
        {
            if (list == null)
            { 
                return false;
            }

            SQLiteConnection connection = null;
            SQLiteTransaction transaction = null;

            try
            {
                connection = _poolSQLiteConnect.ConnectionOpen();

                await using (transaction = connection.BeginTransaction())
                {
                    string command = "INSERT INTO [GeoLocations] (ContinentCode, ContinentName, CountryCode2, CountryCode3, CountryName, CountryNameOfficial, IsEu, StateProv, StateCode, District, City, Zipcode, Latitude, Longitude, DateUpdate) VALUES (@CC, @CN, @CC2, @CC3, @CName, @CNO, @EU, @SP, @SC, @D, @City, @Z, @Lat, @Lon, @DU)";

                    await using (var sqlcommand = new SQLiteCommand(command, connection, transaction))
                    {
                        await foreach (var item in list)
                        {
                            var geo = item.FirstOrDefault();
                            if (geo == null) continue;

                            sqlcommand.Parameters.Clear();
                            sqlcommand.Parameters.AddWithValue("@CC", (object?)geo.ContinentCode ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@CN", (object?)geo.ContinentName ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@CC2", (object?)geo.CountryCode2 ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@CC3", (object?)geo.CountryCode3 ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@CName", (object?)geo.CountryName ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@CNO", (object?)geo.CountryNameOfficial ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@EU", geo.IsEu);
                            sqlcommand.Parameters.AddWithValue("@SP", (object?)geo.StateProv ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@SC", (object?)geo.StateCode ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@D", (object?)geo.District ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@City", (object?)geo.City ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@Z", (object?)geo.Zipcode ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@Lat", (object?)geo.Latitude ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@Lon", (object?)geo.Longitude ?? DBNull.Value);
                            sqlcommand.Parameters.AddWithValue("@DU", date.ToString("yyyy-MM-dd HH:mm:ss"));

                            await sqlcommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }
                    }
                    await transaction.CommitAsync().ConfigureAwait(false);
                }
                return true;
            }
            catch (SQLiteException ex)
            {
                return await _sQLiteExceptionDelegate.RunDelegate(_sQLiteExceptionDelegate.DelegateMethod<GeoLocation>, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Возникло исключение при работе");
                await (transaction?.RollbackAsync() ?? Task.CompletedTask).ConfigureAwait(false);
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
