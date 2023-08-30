using AzureMonitorDataCollectorApiProxy.Misc;
using AzureMonitorDataCollectorApiProxy.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace AzureMonitorDataCollectorApiProxy.Controllers
{
    /// <summary>
    /// Controller for making API calls to the Log Analytics Data Collector API.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DataCollectorController : ControllerBase
    {
        private readonly ILogger<DataCollectorController> _logger;
        private readonly IDataCollectorApi _dataCollectorApi;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectorController"/> class.
        /// </summary>
        /// <param name="logger">ILogger instance for the controller.</param>
        /// <param name="dataCollectorApi">Injected service for working with the Azure Data Collector API.</param>
        public DataCollectorController(ILogger<DataCollectorController> logger, IDataCollectorApi dataCollectorApi)
        {
            _logger = logger;
            _dataCollectorApi = dataCollectorApi;
        }

        /// <summary>
        /// POST custom log(s) to a Log Analytics workspace.
        /// </summary>
        /// <param name="jsonData">POST body in the request.</param>
        [HttpPost]
        [Route("customlog")]
        [ProducesResponseType(typeof(CustomLogPostResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomLogPostResultDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomLogPostResultDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(CustomLogPostResultDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(CustomLogPostResultDto), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(CustomLogPostResultDto), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(CustomLogPostResultDto), StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CustomLog([FromBody] dynamic jsonData)
        {
            string? logTypeHeader = Request.Headers["Log-Type"];
            if (string.IsNullOrEmpty(logTypeHeader))
            {
                CustomLogPostResultDto missingLogTypeDto = new()
                {
                    HttpStatusCode = StatusCodes.Status400BadRequest,
                    ResponseMessage = "Missing required 'Log-Type' request header."
                };

                return StatusCode(400, missingLogTypeDto);
            }

            string stringData = Convert.ToString(jsonData);
            CustomLogPostResultDto dto = await _dataCollectorApi.PostCustomLogAsync(stringData, logTypeHeader).ConfigureAwait(false);
            return StatusCode(dto.HttpStatusCode, dto);
        }
    }
}