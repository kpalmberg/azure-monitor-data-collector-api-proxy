using AzureMonitorDataCollectorApiProxy.Constants;
using AzureMonitorDataCollectorApiProxy.Misc;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace AzureMonitorDataCollectorApiProxy.Controllers
{
    /// <summary>
    /// Error controllers to handle uncaught exceptions via exception handling middleware.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorController"/> class.
        /// </summary>
        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Handles uncaught exceptions and logs the result.
        /// </summary>
        [Route("/error")]
        [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status500InternalServerError)]
        public IActionResult ExceptionHandler()
        {
            var context = HttpContext.Features.Get<IExceptionHandlerFeature>()!;
            var exception = context?.Error;

            _logger.LogError(exception, "An API exception occurred. See exception log for details.");
            ErrorDto errorDto = new()
            { 
                HttpStatusCode = HttpStatusCode.InternalServerError,
                ResponseMessage = PublicFacingErrorMessages.InternalServerError,
            };

            return StatusCode(StatusCodes.Status500InternalServerError, errorDto);
        }
    }
}