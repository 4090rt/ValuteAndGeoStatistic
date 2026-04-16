using EthernetTest.DelegateException;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ValuteAndWeatherStatistic.DelegateException;
using ValuteAndWeatherStatistic.HttpRequests;
using ValuteAndWeatherStatistic.Parser;
using ValuteAndWeatherStatistic.DataBase;
using ValuteAndWeatherStatistic.IHttpCientFactoryDelegate;
using ValuteAndWeatherStatistic.DataBase.InsertRequest;
using ValuteAndWeatherStatistic.ModelData.RequestData;
using ValuteAndWeatherStatistic.DataBase.IntervalTime;

var services = new ServiceCollection();

var delegateClientSettings = new DelegateClientSettings(null);
delegateClientSettings.DelegateCLient1(services);
delegateClientSettings.DelegateCLient2(services);

// Semaphore
services.AddSingleton<SemaphoreSlim>(new SemaphoreSlim(1, 1));

// Делегаты исключений
services.AddSingleton<delegateException>();
services.AddSingleton<delegateHttpException>();
services.AddSingleton<TaskCancelExceptionDelegate>(sp => new TaskCancelExceptionDelegate(sp.GetRequiredService<ILoggerFactory>().CreateLogger("TaskCancelExceptionDelegate")));
services.AddSingleton<JsonExceptionDelegate>(sp => new JsonExceptionDelegate(sp.GetRequiredService<ILoggerFactory>().CreateLogger("JsonExceptionDelegate")));
services.AddSingleton<SQLiteexceptionDelegate>(sp => new SQLiteexceptionDelegate(sp.GetRequiredService<ILoggerFactory>().CreateLogger<SQLiteexceptionDelegate>()));

// Парсер
services.AddSingleton<ParserClass>();

// HTTP запросы
services.AddSingleton<GeoLocationHttpRequest>();
services.AddSingleton<CordinatsHttpRequest>();
services.AddSingleton<ValuteHttpRequest>();
services.AddSingleton<WeathertHttpRequest>();

// База данных
services.AddSingleton<PoolSQLiteConnect>(sp => new PoolSQLiteConnect(sp.GetRequiredService<ILoggerFactory>().CreateLogger("PoolSQLiteConnect")));
services.AddSingleton<InsertRequestCordinats>();
services.AddSingleton<CreateDataBase>();
services.AddSingleton<InsertReqeustWeather>();
services.AddSingleton<InsertRequestCource>();
services.AddSingleton<InsertRequestGeoLoc>();
services.AddSingleton<IntervalDayCource>();
services.AddSingleton<IntervalWeekCource>();
services.AddSingleton<IntervalMounthCurce>();
services.AddSingleton<IntervalDayGeo>();
services.AddSingleton<ItervalWeekGeo>();
services.AddSingleton<IntervalMounthGEO>();

var serviceProvider = services.BuildServiceProvider();

var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
var cts = new CancellationTokenSource();

try
{
    var create = serviceProvider.GetRequiredService<CreateDataBase>();
    await create.Proverka();
    logger.LogWarning("Таблицы созданы!");

    // 1. GeoLocationHttpRequest — получение геолокации
    var geoLocationRequest = serviceProvider.GetRequiredService<GeoLocationHttpRequest>();
    var geoLocationResult = await geoLocationRequest.RequestCache(cts.Token);
    logger.LogInformation("=== GeoLocation ===");
    if (geoLocationResult != null && geoLocationResult.Count > 0)
    {
        foreach (var item in geoLocationResult)
        {
            logger.LogWarning($"City: {item.Geo?.City}, Country: {item.Geo?.CountryName}, Timezone: {item.Timezone}");
        }
    }
    else
    {
        logger.LogWarning("GeoLocation: нет данных");
    }

    var insertGeoLoc = serviceProvider.GetRequiredService<InsertRequestGeoLoc>();
    var saveresultloc = geoLocationResult != null
        ? geoLocationResult.Select(g => g.Geo).Where(g => g != null).Select(g => new List<Geo> { g }).ToAsyncEnumerable()
        : AsyncEnumerable.Empty<List<Geo>>();

    var saveresults = await insertGeoLoc.RequestGeolLoc(saveresultloc, DateTime.Now, cts.Token);
    logger.LogWarning(saveresults ? "Геолокация сохранена в БД" : "Ошибка при сохранении геолокации в БД");


    // 2. CordinatsHttpRequest — получение координат (уже вызывается внутри WeathertHttpRequest)
    // Вызываем отдельно для демонстрации
    var cordinatsRequest = serviceProvider.GetRequiredService<CordinatsHttpRequest>();
    var cordinatsResult = await cordinatsRequest.CacheRequest(cts.Token);
    logger.LogInformation("=== Coordinates ===");
    if (cordinatsResult != null && cordinatsResult.Count > 0)
    {
        foreach (var item in cordinatsResult)
        {
            logger.LogWarning($"City: {item.Geo?.City}, Timezone: {item.Timezone}, Offset: {item.TimezoneOffset}");
        }
    }
    else
    {
        logger.LogWarning("Coordinates: нет данных");
    }

    // Сохранение координат (timezone данные) в базу данных
    var insertCordinats = serviceProvider.GetRequiredService<InsertRequestCordinats>();
    var cordinatsDataList = cordinatsResult != null
        ? new[] { cordinatsResult }.ToAsyncEnumerable()
        : AsyncEnumerable.Empty<List<GeoLocation>>();

    var saveResult = await insertCordinats.CordinatsRequest(cordinatsDataList, DateTime.Now, cts.Token);
    logger.LogWarning(saveResult ? "Координаты сохранены в БД" : "Ошибка при сохранении координат в БД");

    // 3. ValuteHttpRequest — курсы валют
    var valuteRequest = serviceProvider.GetRequiredService<ValuteHttpRequest>();
    var valuteResult = await valuteRequest.RequestCache(cts.Token);
    logger.LogInformation("=== Valute ===");
    if (valuteResult != null && valuteResult.Count > 0)
    {
        foreach (var item in valuteResult)
        {
            logger.LogWarning($"Valute: Result={item.Result}, BaseCode={item.BaseCode}, Rates count={item.ConversionRates?.Count ?? 0}, LastUpdate={item.TimeLastUpdateUtc}");
        }
    }
    else
    {
        logger.LogWarning("Valute: нет данных");
    }

    var coursesavemethod = serviceProvider.GetRequiredService<InsertRequestCource>();
    var saveresult = valuteResult != null
        ? new[] { valuteResult }.ToAsyncEnumerable()
        : AsyncEnumerable.Empty<List<CourceData>>();

    var saved = await coursesavemethod.InserRequest(saveresult, DateTime.Now, cts.Token);
    logger.LogWarning(saved ? "Курсы сохранены в БД" : "Ошибка при сохранении курсов в БД");

    // 4. WeathertHttpRequest — погода (использует координаты внутри)
    var weatherRequest = serviceProvider.GetRequiredService<WeathertHttpRequest>();
    var weatherResult = await weatherRequest.CacheRequest(cts.Token);
    logger.LogInformation("=== Weather ===");
    if (weatherResult != null && weatherResult.Count > 0)
    {
        foreach (var item in weatherResult)
        {
            if (item.Current != null)
            {
                logger.LogWarning($"Weather: Temp={item.Current.Temperature2m}°C, Humidity={item.Current.RelativeHumidity2m}%, Wind={item.Current.WindSpeed10m} km/h");
            }
            else
            {
                logger.LogWarning("Weather: Current data is null");
            }
        }
    }
    else
    {
        logger.LogWarning("Weather: нет данных");
    }

    var savedweather = serviceProvider.GetRequiredService<InsertReqeustWeather>();
    var wathersaeresult = weatherResult != null
        ? new[] { weatherResult }.ToAsyncEnumerable()
        : AsyncEnumerable.Empty<List<WeatherData>>();

    var savedweatherbd = await savedweather.WeatherRequest(wathersaeresult, DateTime.Now, cts.Token);
    logger.LogWarning(saved ? "Погода  сохранена в БД" : "Ошибка при сохранении погоды в БД");
}
catch (Exception ex)
{
    logger.LogError(ex, "Ошибка при выполнении запросов");
}

Console.WriteLine("Запросы завершены. Нажмите любую клавишу...");
Console.ReadKey();
