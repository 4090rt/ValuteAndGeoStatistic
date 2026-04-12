using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValuteAndWeatherStatistic.DelegateException
{
    public delegate Task<List<T>> DELEGATEhTTPeXCEPTION<T>(HttpRequestException ex);
    public class delegateHttpException
    {
        private readonly ILogger<delegateHttpException> _logger;

        public delegateHttpException(ILogger<delegateHttpException> logger)
        {
            _logger = logger;
        }

        public async Task<List<T>> RunDelegate<T>(DELEGATEhTTPeXCEPTION<T> dELEGATEhTTPe, HttpRequestException ex)
        {
            var result = await dELEGATEhTTPe.Invoke(ex).ConfigureAwait(false);
            return result;
        }

        public async Task<List<T>> Delegate<T>(HttpRequestException ex)
        {
            _logger.LogError(ex, "Возникло исключение во время запроса" + ex.Message + ex.StackTrace);
            return new List<T>();
        }
    }
}
