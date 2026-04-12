using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValuteAndWeatherStatistic.DelegateException
{
    public delegate Task<T> DelegateExceptionSQL<T>(SQLiteException ex);
    public class SQLiteexceptionDelegate
    {
        private readonly ILogger<SQLiteexceptionDelegate> _logger;

        public SQLiteexceptionDelegate(ILogger<SQLiteexceptionDelegate> logger)
        {
            _logger = logger;
        }

        public async Task<T> RunDelegate<T>(DelegateExceptionSQL<T> deleagte, SQLiteException ex)
        { 
            var result = await deleagte.Invoke(ex);
            return result;
        }

        public async Task<List<T>> DelegateMethod<T>(SQLiteException ex)
        {
            _logger.LogError(ex, "Возникло исключение при работе с БД");
            return new List<T>();
        }
    }
}
