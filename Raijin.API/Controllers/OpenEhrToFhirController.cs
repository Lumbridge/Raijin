using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using Raijin.Core.Classes;
using Raijin.Core.Helpers;

namespace Raijin.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OpenEhrToFhirController : ControllerBase
    {
        private readonly ILogger<OpenEhrToFhirController> _logger;

        public OpenEhrToFhirController(ILogger<OpenEhrToFhirController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public ActionResult Post([FromBody]string openEhrMessage)
        {
            if (string.IsNullOrEmpty(openEhrMessage))
                return BadRequest();

            var acceptHeader = Request.Headers["Accept"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(acceptHeader))
                acceptHeader = "application/json";

            var messageHandler = new MessageHandler();
            
            var processedMessage = messageHandler.ProcessMessage(openEhrMessage);

            var result = acceptHeader.Contains("xml")
                ? processedMessage.SerializedResourceBundleXml
                : processedMessage.SerializedResourceBundleJson;

            return Content(result); 
        }
    }
}
