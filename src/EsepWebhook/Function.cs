using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook
{
    public class Function
    {
        private static readonly HttpClient Http = new HttpClient();
        private static readonly string SlackUrl = Environment.GetEnvironmentVariable("SLACK_URL") ?? "";

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                // Parse the GitHub webhook payload
                var body = request?.Body ?? "{}";
                var json = JObject.Parse(body);

                var issueUrl = json.SelectToken("issue.html_url")?.ToString();
                var title = json.SelectToken("issue.title")?.ToString();
                var action = json.SelectToken("action")?.ToString();
                var repo = json.SelectToken("repository.full_name")?.ToString();

                if (!string.IsNullOrEmpty(SlackUrl) && !string.IsNullOrEmpty(issueUrl))
                {
                    var message = new
                    {
                        text = $"🧩 Issue *{action}* in *{repo}*:\n*{title}*\n<{issueUrl}|View on GitHub>"
                    };

                    var payload = System.Text.Json.JsonSerializer.Serialize(message);
                    var resp = await Http.PostAsync(SlackUrl, new StringContent(payload, Encoding.UTF8, "application/json"));
                    resp.EnsureSuccessStatusCode();
                }

                return new APIGatewayProxyResponse
                {
                    StatusCode = 200,
                    Body = "{\"ok\": true}"
                };
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error: {ex}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = 500,
                    Body = "{\"ok\": false}"
                };
            }
        }
    }
}
