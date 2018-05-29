using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Cognitive.CustomVision.Prediction;
using Microsoft.Cognitive.CustomVision.Prediction.Models;
using Newtonsoft.Json;

namespace AerialObjectRecognitionFunction
{
    public static class OnNewImage
    {
        [FunctionName("OnNewImage")]
        public static async Task<object> Run([HttpTrigger(WebHookType = "genericJson")]HttpRequestMessage req, TraceWriter log)
        {
            try
            {
                log.Info($"Webhook was triggered!");

                // Get request body
                dynamic data = await req.Content.ReadAsAsync<object>();
                
                // Complete image url
                string imagePath = data.path;
                string imageUrl = $"https://{ConfigurationManager.AppSettings["ImagesStorageAccountName"]}.blob.core.windows.net{imagePath}";
                log.Info(imageUrl);

                // evaluate image
                bool objectFound = await ObjectFound(imageUrl);
                log.Info($"object found? {objectFound}");

                // Track event on Application Insights
                string instrumentationKey = ConfigurationManager.AppSettings["APPINSIGHTS_INSTRUMENTATIONKEY"];

                if (!string.IsNullOrEmpty(instrumentationKey))
                {
                    log.Info("Tracking event to Application Insights");

                    TelemetryClient _telemetryClient = new TelemetryClient(
                        new Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration(instrumentationKey));

                    if (objectFound)
                        _telemetryClient.TrackEvent("Object");
                    else
                        _telemetryClient.TrackEvent("Oean");
                }

                // Create output object
                Models.Output output = new Models.Output()
                {
                    ObjectFound = objectFound,
                    ImageUrl = imageUrl,
                };

                // return result
                return req.CreateResponse(HttpStatusCode.OK, output);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Uses Cognitive Services Custom Vision API to determine whether an object was found in an image
        /// with some degree of certainty
        /// </summary>
        /// <param name="imageUrl"></param>
        /// <returns>Boolean</returns>
        private static async Task<bool> ObjectFound(string imageUrl, double minimumScore = 0.5)
        {
            bool objectFound = false;

            try
            {
                // Initialize prediction endpoint
                PredictionEndpoint predictionEndpoint = new PredictionEndpoint()
                {
                    ApiKey = ConfigurationManager.AppSettings["CustomVisionApiKey"]
                };

                // Call custom vision prediction API to predict image
                ImagePredictionResultModel predictionResult = await predictionEndpoint.PredictImageUrlAsync(
                    new Guid(ConfigurationManager.AppSettings["CustomVisionProjectId"]),
                    new ImageUrl(imageUrl));

                // Query for the object tag
                var objectTag = predictionResult.Predictions.Where(x => x.Tag == "Object").FirstOrDefault();

                // Check if the object tag probability surpassed the minimum score
                if (objectTag != null && objectTag.Probability >= minimumScore)
                    objectFound = true;
            }
            catch (Exception e)
            {
                throw e;
            }

            // Return result
            return objectFound;
        }
    }
}
