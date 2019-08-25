using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Polly.CircuitBreaker;

namespace Orchestrator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {

        private readonly HttpClient httpClient;

        public ValuesController(IHttpClientFactory httpClientFactory)
        {
            httpClient = httpClientFactory.CreateClient("functionAClient");
        }

        // GET api/values
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                string name = "sriks";

                var requestURL = "?name=" + name;

                var responseMessage = await httpClient.GetAsync(requestURL);

                if (responseMessage != null && responseMessage.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return (ActionResult)new OkObjectResult($"Response from Orchestrator: {responseMessage.Content.ReadAsStringAsync()}");
                }
                else
                {
                    return HandledResponse(responseMessage);
                }
            }
            catch (BrokenCircuitException ex)
            {
                return new BadRequestObjectResult("Function A is inoperative, please try later on. (Business message due to Circuit-Breaker)");
            }
        }

        // Converts HttpResponseMessage to IActionResult
        private IActionResult HandledResponse(HttpResponseMessage responseMessage)
        {
            if (responseMessage.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return new BadRequestObjectResult(responseMessage.Content.ReadAsStringAsync());
            }
            return new BadRequestObjectResult("Unknown error");
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
