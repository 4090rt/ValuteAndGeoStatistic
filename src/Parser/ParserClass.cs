using EthernetTest.DelegateException;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ValuteAndWeatherStatistic.DelegateException;

namespace ValuteAndWeatherStatistic.Parser
{
    public class ParserClass
    {
        private readonly ILogger<ParserClass> _logger;
        private readonly delegateException _delegateException;
        private readonly JsonExceptionDelegate _jsonExceptionDelegate;

        public ParserClass (ILogger<ParserClass> logger, delegateException delegateException, JsonExceptionDelegate jsonExceptionDelegate)
        {
            _logger = logger;
            _delegateException = delegateException;
            _jsonExceptionDelegate = jsonExceptionDelegate;
        }

        public async Task<List<T>> Parser<T>(Stream stream)
        {
            try
            {
                var options = new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                };

                var deserialize = await JsonSerializer.DeserializeAsync<List<T>>(stream, options);

                if (deserialize != null)
                {
                    return deserialize;
                }
                else
                {
                    _logger.LogError("Ошибка парснига списка");
                    return new List<T>();
                }
            }
            catch (JsonException ex)
            {
                return await _jsonExceptionDelegate.DelegateRun(_jsonExceptionDelegate.ExceptioMethod<T>, ex).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return await _delegateException.RunDelegate(_delegateException.Delegate<T>, ex).ConfigureAwait(false);
            }
        }

        public async Task<List<T>> ParserObject<T>(Stream stream)
        {
            try
            {
                var options = new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy =  JsonNamingPolicy.CamelCase
                };

                using var type = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);

                _logger.LogInformation("ParserObject: ValueKind = {ValueKind}", type.RootElement.ValueKind);

                if (type.RootElement.ValueKind == JsonValueKind.Object)
                {
                    var raw = type.RootElement.GetRawText();
                    _logger.LogInformation("ParserObject: Raw JSON (first 300) = {Raw}", raw.Length > 300 ? raw.Substring(0, 300) + "..." : raw);
                    var deserialize = JsonSerializer.Deserialize<T>(raw, options);
                    _logger.LogInformation("ParserObject: Успешно распаршено (объект), Result = {Result}", deserialize != null ? "not null" : "null");
                    return deserialize != null ? new List<T> { deserialize } : new List<T>();
                }
                else if (type.RootElement.ValueKind == JsonValueKind.Array)
                {
                   var raw = type.RootElement.GetRawText();
                   var deserialize = JsonSerializer.Deserialize<List<T>>(raw, options);
                   return deserialize ?? new List<T>();
                }
                else
                {
                    _logger.LogError("Ошибка парсинга, необрабатываемый тип при попытке распарсить: {ValueKind}", type.RootElement.ValueKind);
                    return new List<T>();
                }
            }
            catch (JsonException ex)
            {
              return await _jsonExceptionDelegate.DelegateRun(_jsonExceptionDelegate.ExceptioMethod<T>, ex);
            }
            catch (Exception ex)
            {
               return await _delegateException.RunDelegate(_delegateException.Delegate<T>, ex);
            }
        }
    }
}
