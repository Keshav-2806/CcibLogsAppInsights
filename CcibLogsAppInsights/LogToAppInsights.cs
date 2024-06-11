using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace CcibLogsAppInsights
{
    public static class LogToAppInsights
    {
        private static readonly TelemetryClient telemetryClient;

        static LogToAppInsights()
        {
            string instrumentationKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");
            if (string.IsNullOrEmpty(instrumentationKey))
            {
                throw new InvalidOperationException("Application Insights instrumentation key is missing.");
            }
            TelemetryConfiguration configuration = TelemetryConfiguration.CreateDefault();
            configuration.InstrumentationKey = instrumentationKey;
            telemetryClient = new TelemetryClient(configuration);
        }

        [FunctionName("LogToAppInsights")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);

                string messageId = data?.MessageId;
                string status = data?.Status;
                string sender = data?.Sender;
                string receiver = data?.Receiver;
                string comments=data?.Comments;


                if (string.IsNullOrEmpty(messageId) || string.IsNullOrEmpty(status) || string.IsNullOrEmpty(sender) || string.IsNullOrEmpty(receiver))
                {
                    return new BadRequestObjectResult("Please pass MessageID, Status, Sender and Receiver in the request body.");
                }
                var telemetryProperties = new Dictionary<string, string>
            {
                {"MessageId",messageId},
                {"Status",status},
                {"Sender",sender},
                {"Receiver",receiver},
                {"Comments",comments }
            };
                telemetryClient.TrackEvent("LogicAppEvent", telemetryProperties);

                return new OkObjectResult("Logged to Application Insights");
            }
            catch (Exception ex)
            {
                log.LogError($"Exception occurred: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
