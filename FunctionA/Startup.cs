using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Diagnostics;
using System.Net.Http;

[assembly: FunctionsStartup(typeof(FunctionA.Startup))]
namespace FunctionA
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient("functionBClient", client =>
            {
                client.BaseAddress = new Uri("http://localhost:7072/api/FunctionB");
            })
            .SetHandlerLifetime(TimeSpan.FromSeconds(5))  //Sample. Default lifetime is 2 minutes
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());
            builder.Services.Configure<HttpClientFactoryOptions>(options => options.SuppressHandlerScope = true);
        }

        static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
              .HandleTransientHttpError() // handles HTTP 5xx responses, and HTTP 408 responses.
              .WaitAndRetryAsync(3, // sample only 
              retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
              onRetry: (response, calculatedWaitDuration) =>
               {
                   Debug.WriteLine($"Failed attempt. Waited for {calculatedWaitDuration}. Retrying. {response}");
               }
            );
        }

        static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                3, // this should be same as retry count
                TimeSpan.FromSeconds(30),
                onBreak: (response, timeSpan) =>
                {
                    Debug.WriteLine($"Response {response}, timeSpan {timeSpan}");
                },
                onReset: () =>
                {
                    Debug.WriteLine($"Reset fired");
                },
                onHalfOpen: () =>
                {
                    Debug.WriteLine($"half open fired");
                }
                );
        }
    }
}
