using AzureMonitorDataCollectorApiProxy.Misc;
using AzureMonitorDataCollectorApiProxy.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace AzureMonitorDataCollectorApiProxy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataCollectorController : ControllerBase
    {
        private readonly ILogger<DataCollectorController> _logger;
        private readonly IDataCollectorApi _dataCollectorApi;

        public DataCollectorController(ILogger<DataCollectorController> logger, IDataCollectorApi dataCollectorApi)
        {
            _logger = logger;
            _dataCollectorApi = dataCollectorApi;
        }

        [HttpPost]
        [Route("customlog")]
        public async Task<IActionResult> CustomLog([FromBody] dynamic jsonData)
        {
            string logTypeHeader = Request.Headers["Log-Type"];
            if (logTypeHeader == null)
            {
                return StatusCode(400, "Missing required 'Log-Type' request header.");
            }

            string stringData = Convert.ToString(jsonData);
            CustomLogPostResultDto dto = await _dataCollectorApi.PostCustomLogAsync(stringData, logTypeHeader).ConfigureAwait(false);
            return StatusCode((int)dto.HttpStatusCode, dto.ResponseMessage);
        }
    }
}
