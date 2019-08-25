using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using Polly.CircuitBreaker;

namespace FunctionA
{
    public class FunctionA
    {
        private readonly HttpClient _httpClient;

        public FunctionA(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("functionBClient");
        }

        [FunctionName("FunctionA")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation("C# HTTP trigger function processed a request.");

                string name = req.Query["name"];

                var requestURL = "?name=" + name;

                HttpResponseMessage responseMessage = await _httpClient.GetAsync(requestURL);

                if (responseMessage != null && responseMessage.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return (ActionResult)new OkObjectResult($"Hello {name} from Function A \n {responseMessage.Content.ReadAsStringAsync()}");
                }
                else
                {
                    return HandleResponse(responseMessage);
                }
            }
            catch (BrokenCircuitException ex)
            {
                log.LogError("Function B is inoperative, please try later on. (Business message due to Circuit-Breaker)", ex.Message);
                return new BadRequestObjectResult("Function B is inoperative, please try later on. (Business message due to Circuit-Breaker)");
            }
        }

        // Converts HttpResponseMessage to IActionResult
        private IActionResult HandleResponse(HttpResponseMessage responseMessage)
        {
            if(responseMessage.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                // add any additional data here.
                return new BadRequestObjectResult(responseMessage.Content.ReadAsStringAsync());
            }
            return new BadRequestObjectResult("Unknown error");
        }
    }
}
