using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValuteAndWeatherStatistic.DataBase
{
    public class CreateDataBase
    {
        private readonly ILogger<CreateDataBase> _logger;
        private readonly PoolSQLiteConnect _poolSQLiteConnect;
        private bool? _isCheckedCreate;

        public CreateDataBase(ILogger<CreateDataBase> logger, PoolSQLiteConnect poolSQLiteConnect)
        { 
            _logger = logger;
            _poolSQLiteConnect = poolSQLiteConnect;
        }
        public async Task Proverka()
        {
            if (_isCheckedCreate == true) return;

            bool create1 = await CreateDataBaseMethod1();
            bool create2 = await CreateDataBaseMethod2();
            bool create3 = await CreateDataBaseMethod3();
            bool create4 = await CreateDataBaseMethod4();

            if (create1 == false || create2 == false || create3 == false || create4 == false)
            {
                _logger.LogWarning("Не удалось создать БД при проверке или не удалось создать одну из таблиц");
            }

            _isCheckedCreate = true;
        }
        public async Task<bool> CreateDataBaseMethod1()
        {
            SQLiteConnection connection = null;
            try
            {
                connection = _poolSQLiteConnect.ConnectionOpen();

                string command = "CREATE TABLE IF NOT EXISTS Coordinates (" +
                        "Id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                        "GeoLocation TEXT, " +
                        "GeoCountry TEXT, " +
                        "GeoState TEXT, " +
                        "GeoCity TEXT, " +
                        "GeoLocality TEXT, " +
                        "GeoLatitude TEXT, " +
                        "GeoLongitude TEXT, " +
                        "Timezone TEXT, " +
                        "TimezoneOffset INTEGER, " +
                        "TimezoneOffsetWithDst INTEGER, " +
                        "Date TEXT, " +
                        "DateTime TEXT, " +
                        "DateTimeTxt TEXT, " +
                        "DateTimeWti TEXT, " +
                        "DateTimeYmd TEXT, " +
                        "DateTimeUnix REAL, " +
                        "Time24 TEXT, " +
                        "Time12 TEXT, " +
                        "Week INTEGER, " +
                        "Month INTEGER, " +
                        "Year INTEGER, " +
                        "YearAbbr TEXT, " +
                        "CurrentTzAbbreviation TEXT, " +
                        "CurrentTzFullName TEXT, " +
                        "StandardTzAbbreviation TEXT, " +
                        "StandardTzFullName TEXT, " +
                        "IsDst INTEGER, " +
                        "DstSavings INTEGER, " +
                        "DstExists INTEGER, " +
                        "DstTzAbbreviation TEXT, " +
                        "DstTzFullName TEXT, " +
                        "DstStart TEXT, " +
                        "DstEnd TEXT, " +
                        "DateUpdate TEXT" +
                    ");";

                using (var sqlcommand = new SQLiteCommand(command, connection))
                {
                    await sqlcommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                    _logger.LogCritical("Таблица 1 (Coordinates) создана/проверена успешно");
                }
                return true;
            }
            catch (SQLiteException ex)
            {
                _logger.LogError(ex, "Возникло исключение при работе с БД (Coordinates)");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при попытке создания таблицы Coordinates");
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

        public async Task<bool> CreateDataBaseMethod2()
        {
            SQLiteConnection connection = null;
            try
            {
                connection = _poolSQLiteConnect.ConnectionOpen();

                string command = "CREATE TABLE IF NOT EXISTS WeatherCurrent (" +
                        "Id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                        "CoordinatesId INTEGER NOT NULL, " +
                        "Latitude REAL, " +
                        "Longitude REAL, " +
                        "GenerationTimeMs REAL, " +
                        "UtcOffsetSeconds INTEGER, " +
                        "Timezone TEXT, " +
                        "TimezoneAbbreviation TEXT, " +
                        "Time TEXT NOT NULL, " +
                        "Interval INTEGER, " +
                        "Temperature REAL, " +
                        "ApparentTemperature REAL, " +
                        "RelativeHumidity INTEGER, " +
                        "Precipitation REAL, " +
                        "WeatherCode INTEGER, " +
                        "WindSpeed REAL, " +
                        "WindDirection INTEGER, " +
                        "DateUpdate TEXT, " +
                        "FOREIGN KEY(CoordinatesId) REFERENCES Coordinates(Id)" +
                    ");";

                using (var sqlcommand = new SQLiteCommand(command, connection))
                {
                    await sqlcommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                    _logger.LogCritical("Таблица 2 (WeatherCurrent) создана/проверена успешно");
                }
                return true;
            }
            catch (SQLiteException ex)
            {
                _logger.LogError(ex, "Возникло исключение при работе с БД (WeatherCurrent)");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при попытке создания таблицы WeatherCurrent");
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

        public async Task<bool> CreateDataBaseMethod3()
        {
            SQLiteConnection connection = null;
            try
            {
                connection = _poolSQLiteConnect.ConnectionOpen();

                string command =
                    "CREATE TABLE IF NOT EXISTS CurrencyRates (" +
                        "Id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                        "Result TEXT, " +
                        "TimeLastUpdateUtc TEXT, " +
                        "TimeNextUpdateUtc TEXT, " +
                        "BaseCode TEXT NOT NULL, " +
                        "ConversionRates TEXT NOT NULL, " +
                        "DateUpdate TEXT" +
                    ");";

                using (var sqlcommand = new SQLiteCommand(command, connection))
                {
                    await sqlcommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                    _logger.LogCritical("Таблица 3 (CurrencyRates) создана/проверена успешно");
                }
                return true;
            }
            catch (SQLiteException ex)
            {
                _logger.LogError(ex, "Возникло исключение при работе с БД (CurrencyRates)");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при попытке создания таблицы CurrencyRates");
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

        public async Task<bool> CreateDataBaseMethod4()
        {
            SQLiteConnection connection = null;
            try
            {
                connection = _poolSQLiteConnect.ConnectionOpen();

                string command =
                    "CREATE TABLE IF NOT EXISTS GeoLocations (" +
                        "Id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                        "ContinentCode TEXT, " +
                        "ContinentName TEXT, " +
                        "CountryCode2 TEXT, " +
                        "CountryCode3 TEXT, " +
                        "CountryName TEXT, " +
                        "CountryNameOfficial TEXT, " +
                        "IsEu INTEGER, " +
                        "StateProv TEXT, " +
                        "StateCode TEXT, " +
                        "District TEXT, " +
                        "City TEXT, " +
                        "Zipcode TEXT, " +
                        "Latitude TEXT, " +
                        "Longitude TEXT, " +
                        "DateUpdate TEXT" +
                    ");";

                using (var sqlcommand = new SQLiteCommand(command, connection))
                {
                    await sqlcommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                    _logger.LogCritical("Таблица 4 (GeoLocations) создана/проверена успешно");
                }
                return true;
            }
            catch (SQLiteException ex)
            {
                _logger.LogError(ex, "Возникло исключение при работе с БД (GeoLocations)");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при попытке создания таблицы GeoLocations");
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
