using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Net;


namespace ValuteAndWeatherStatistic.IHttpCientFactoryDelegate
{
    public delegate void serviceCollections(IServiceCollection servicecollections);
    public class DelegateClientSettings
    {
        private readonly ILogger<DelegateClientSettings> _logger;

        public DelegateClientSettings(ILogger<DelegateClientSettings> logger)
        {
            _logger = logger;
        }

        public void RunDelegate(serviceCollections service, IServiceCollection httpClientFactory)
        { 
             service.Invoke(httpClientFactory);
        }

        public void DelegateCLient1(IServiceCollection serviceDescriptors)
        {
            serviceDescriptors.AddLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Warning);
            });

            serviceDescriptors.AddMemoryCache();
            serviceDescriptors.AddHttpClient("ClientHttp", client =>
            {
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                client.DefaultRequestVersion = HttpVersion.Version20;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            }).AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(
                TimeSpan.FromSeconds(10),
                Polly.Timeout.TimeoutStrategy.Pessimistic,
                onTimeoutAsync: (context, timespan, task) =>
                {
                    Console.WriteLine($"⏰ Request timed out after {timespan}");
                    return Task.CompletedTask;
                })).AddTransientHttpErrorPolicy(policy => policy.CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromMinutes(1),
                    onBreak: (outcome, timespan) =>
                    {
                        Console.WriteLine($"🔌 Circuit opened for {timespan}");
                    },
                    onHalfOpen: () =>
                    {
                        Console.WriteLine("⚠️ Circuit half-open");
                    },
                    onReset: () =>
                    {
                        Console.WriteLine("✅ Circuit reset");
                    })).AddTransientHttpErrorPolicy(polly => polly.WaitAndRetryAsync
                    (3, retrycount => TimeSpan.FromSeconds(Math.Pow(2, retrycount)) + TimeSpan
                    .FromMilliseconds(Random.Shared.Next(0, 100)),
                    onRetry: async (outcome, timespan, retrycount, context) =>
                    {
                        Console.WriteLine($"🔄 Retry {retrycount} after {timespan}");
                    })).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
                    {
                        EnableMultipleHttp2Connections = true,

                        PooledConnectionLifetime = TimeSpan.FromMinutes(15),
                        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(10),

                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,

                        MaxConnectionsPerServer = 10,
                        UseCookies = false,
                        AllowAutoRedirect = false,
                    });
        }
        public void DelegateCLient2(IServiceCollection serviceDescriptors)
        {
            serviceDescriptors.AddLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Warning);
            });

            serviceDescriptors.AddMemoryCache();
            serviceDescriptors.AddHttpClient("HttpClient2", client =>
            {
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                client.DefaultRequestVersion = HttpVersion.Version20;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            }).AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>
            (
              TimeSpan.FromSeconds(15),
              Polly.Timeout.TimeoutStrategy.Pessimistic,
              onTimeoutAsync: (context, timespan, task) =>
              {
                  Console.WriteLine($"⏰ Request timed out after {timespan}");
                  return Task.CompletedTask;
              })).AddTransientHttpErrorPolicy(policy => policy.CircuitBreakerAsync(
                  handledEventsAllowedBeforeBreaking: 5,
                  durationOfBreak: (TimeSpan.FromMinutes(1)),
                  onBreak: (outcome, timespan) =>
                  {
                      Console.WriteLine($"🔌 Circuit opened for {timespan}");
                  },
                  onHalfOpen: () =>
                  {
                      Console.WriteLine("⚠️ Circuit half-open");
                  },
                  onReset: () =>
                  {
                      Console.WriteLine("✅ Circuit reset");
                  })).AddTransientHttpErrorPolicy(polly => polly.WaitAndRetryAsync
                    (3, retrycount => TimeSpan.FromSeconds(Math.Pow(2, retrycount)) + TimeSpan
                    .FromMilliseconds(Random.Shared.Next(0, 100)),
                    onRetry: async (outcome, timespan, retrycount, context) =>
                    {
                        Console.WriteLine($"🔄 Retry {retrycount} after {timespan}");
                    })).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
                    {
                        EnableMultipleHttp2Connections = true,

                        PooledConnectionLifetime = TimeSpan.FromMinutes(15),
                        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(10),

                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,

                        MaxConnectionsPerServer = 10,
                        UseCookies = false,
                        AllowAutoRedirect = false,
                    });
        }

        public void DelegateCLient3(IServiceCollection serviceDescriptors)
        {
            serviceDescriptors.AddLogging(logger =>
            {
                logger.AddConsole();
                logger.SetMinimumLevel(LogLevel.Warning);
            });

            serviceDescriptors.AddMemoryCache();
            serviceDescriptors.AddHttpClient("HttpClient3", client =>
            {
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                client.DefaultRequestVersion = HttpVersion.Version11;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
            }).AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(
                TimeSpan.FromSeconds(15),
                Polly.Timeout.TimeoutStrategy.Pessimistic,
                onTimeoutAsync: (context, timespan, task) =>
                {
                    Console.WriteLine($"⏰ Request timed out after {timespan}");
                    return Task.CompletedTask;
                })).AddTransientHttpErrorPolicy(polly => polly.CircuitBreakerAsync
                (handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, timespan) =>
                {
                    Console.WriteLine($"🔌 Circuit opened for {timespan}");
                },
                onHalfOpen: () =>
                {
                    Console.WriteLine("⚠️ Circuit half-open");
                },
                onReset: () =>
                {
                    Console.WriteLine("✅ Circuit reset");
                })).AddTransientHttpErrorPolicy(policy => 
                policy.WaitAndRetryAsync(3, retycount => TimeSpan.FromSeconds(Math.Pow(2,retycount))
                + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)), 
                onRetry: async (outcome, timespan, retrycount, context) =>
                {
                    Console.WriteLine($"🔄 Retry {retrycount} after {timespan}");
                }));
        }
    }
}
