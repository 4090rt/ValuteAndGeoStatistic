using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValuteAndWeatherStatistic.DelegateException
{
    public delegate Task<List<T>> delegateException<T>(Exception ex);
    public class delegateException
    {
        private readonly ILogger<delegateException> _logger;

        public delegateException(ILogger<delegateException> logger)
        {
            _logger = logger;
        }

        public async Task<List<T>> RunDelegate<T>(delegateException<T> delegateException, Exception ex)
        {
            var result = await delegateException.Invoke(ex).ConfigureAwait(false);
            return result;
        }

        public async Task<List<T>> Delegate<T>(Exception ex)
        {
            _logger.LogInformation("Возникло исключение" + ex.Message + ex.StackTrace);
            return new List<T>();
        }
    }
}
