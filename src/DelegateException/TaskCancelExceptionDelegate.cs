using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EthernetTest.DelegateException
{
    public delegate Task<List<T>> DelegateException<T>(TaskCanceledException ex);
    public class TaskCancelExceptionDelegate
    {
        private readonly ILogger _logger;

        public TaskCancelExceptionDelegate(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<List<T>> RunDelegate<T>(DelegateException<T> delegateException, TaskCanceledException ex)
        { 
            return await delegateException.Invoke(ex);
        }

        public async Task<List<T>> ExceptionMethod<T>(TaskCanceledException ex)
        {
            _logger.LogError(ex, "Операция отменена" + ex.Message + ex.StackTrace);
            return new List<T>();
        }
    }
}
