using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;


namespace EM
{
    public static class hacker3FAOpenHack
    {
        [FunctionName("hacker3FAOpenHack")]
        public static async Task<IActionResult> RunFATest(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string productId = req.Query["productId"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            productId = productId ?? data?.productId;

            string responseMessage = string.IsNullOrEmpty(productId)
                ? "This HTTP triggered function executed successfully. Pass a name in the product id or in the request body for a personalized response."
                : $"The product name for your product id {productId} is Starfruit Explosion.";

            return new OkObjectResult(responseMessage);
        }


        [FunctionName("CreateRating")]
        public static async Task<IActionResult> RunCreateRating(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName: "Ratings",
                collectionName: "ratingItems",
                ConnectionStringSetting = "ConnectionStringSetting")] IAsyncCollector<object>  document,
            ILogger log)
        {
            log.LogInformation("CreateRating function processed a request.");            

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string userId = data?.userId;
            string productId = data?.productId;
            string locationName = data?.locationName;
            string rating = data?.rating;
            string userNotes = data?.userNotes;

            if(int.Parse(rating) < 0 && int.Parse(rating) > 5)
            {
                return new BadRequestObjectResult("RAting should be between 0 to 5");
            }

            HttpClient client = new HttpClient();
             try	
                {
                    string urlForProductId = $"https://serverlessohapi.azurewebsites.net/api/GetProduct?productId={productId}";
                    HttpResponseMessage response = await client.GetAsync(urlForProductId);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    // Above three lines can be replaced with new helper method below
                    // string responseBody = await client.GetStringAsync(uri);

                    Console.WriteLine(responseBody);
                }
                catch(HttpRequestException e)
                {
                    return new BadRequestObjectResult("ProductId not found");
                }

                try	
                {
                    string urlForUserId = $"https://serverlessohapi.azurewebsites.net/api/GetUser?userId={userId}";
                    HttpResponseMessage response = await client.GetAsync(urlForUserId);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    // Above three lines can be replaced with new helper method below
                    // string responseBody = await client.GetStringAsync(uri);

                    Console.WriteLine(responseBody);
                }
                catch(HttpRequestException e)
                {
                    return new BadRequestObjectResult("UserId not found");
                }

                string id = Guid.NewGuid().ToString();
                var timeStamp = DateTime.Now.ToString();     

                 var ratingdocument = new {
                    id = id,
                    userId = userId,
                    productId = productId,
                    locationName = locationName,
                    timeStamp = timeStamp,
                    rating = rating,
                    userNotes = userNotes
      };

      await document.AddAsync(ratingdocument);
      //Console.WriteLine(response1.ToString());



            // string responseMessage = string.IsNullOrEmpty(productId)
            //     ? "This HTTP triggered function executed successfully. Pass a name in the product id or in the request body for a personalized response."
            //     : $"The product name for your product id {productId} is Starfruit Explosion.";

            return new OkObjectResult (id);
        }

        [FunctionName("GetRating")]
        public static IActionResult RunGetRating(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "rating/{id}")]HttpRequest req,
            [CosmosDB(
                databaseName: "Ratings",
                collectionName: "ratingItems",
                ConnectionStringSetting = "ConnectionStringSetting",
                SqlQuery = "select * from ratingItems c where c.id = {id}")] IEnumerable<object>  documents,
            ILogger log)
        {
            log.LogInformation("GetRating function processed a request.");         

            if (documents == null)
            {
                return new NotFoundResult();
            }
            else
            {
                return new OkObjectResult(documents);
            }

        }

         [FunctionName("GetRatings")]
        public static IActionResult RunGetRatings(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ratings/{userId}")]HttpRequest req,
            [CosmosDB(
                databaseName: "Ratings",
                collectionName: "ratingItems",
                ConnectionStringSetting = "ConnectionStringSetting",
                SqlQuery = "select * from ratingItems c where c.userId = {userId}")] IEnumerable<object>  documents,
            ILogger log)
        {
            log.LogInformation("GetRatings function processed a request.");         


            if ((documents == null) || (documents.Count() == 0))
            {
                return new BadRequestObjectResult("UserId not found");
            }
            else
            {
                return new OkObjectResult(documents);
            }

        }
    }
}
