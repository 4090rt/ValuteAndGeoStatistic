using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EthernetTest.DelegateException
{
    public delegate Task<List<T>> JsonSerealizeDelegate<T>(JsonException ex);
    public class JsonExceptionDelegate
    {
        private readonly ILogger _logger;

        public JsonExceptionDelegate(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<List<T>> DelegateRun<T>(JsonSerealizeDelegate<T> jsonExceptionDelegate, JsonException ex)
        {
            return await jsonExceptionDelegate.Invoke(ex);
        }

        public async Task<List<T>> ExceptioMethod<T>(JsonException ex)
        {
            _logger.LogError("Возникло исключение при работе с JSON: " + ex.Message + ex.StackTrace);
            return new List<T>();
        }
    }
}
