using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using server.Classes.ClientHandler;
using server.Interface;

namespace server.Classes.ClientHandler
{
    public class HttpRequestHandler
    {
        private readonly IClientManager _clientManager;
        private readonly ClientSettingsService _clientSettingsService;

        public HttpRequestHandler(IClientManager clientManager, ClientSettingsService clientSettingsService)
        {
            _clientManager = clientManager;
            _clientSettingsService = clientSettingsService;
        }

        public async Task HandleRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                AddCorsHeaders(response);

                if (request.HttpMethod == "OPTIONS")
                {
                    HandleOptionsRequest(response);
                    return;
                }

                if (request.Url.AbsolutePath.StartsWith("/api/client/"))
                {
                    switch (request.HttpMethod)
                    {
                        case "PUT":
                            await HandlePutRequest(request, response);
                            break;
                        case "GET":
                            await HandleGetRequest(request, response);
                            break;
                        default:
                            response.StatusCode = 405; // Method Not Allowed
                            break;
                    }
                }
                else
                {
                    response.StatusCode = 404; // Not Found
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling request: {ex.Message}");
                response.StatusCode = 500; // Internal Server Error
            }
            finally
            {
                response.Close();
            }
        }

        private void AddCorsHeaders(HttpListenerResponse response)
        {
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Methods", "GET, PUT, OPTIONS");
            response.AddHeader("Access-Control-Allow-Headers", "Content-Type");
            response.AddHeader("Cache-Control", "no-store, no-cache, must-revalidate, max-age=0");
            response.AddHeader("Cache-Control", "post-check=0, pre-check=0");
            response.AddHeader("Pragma", "no-cache");
        }

        private void HandleOptionsRequest(HttpListenerResponse response)
        {
            response.StatusCode = 200; // OK
        }

        private async Task HandlePutRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            var pathParts = request.Url.AbsolutePath.Trim('/').Split('/');

            if (pathParts.Length == 4 && pathParts[3] == "settings")
            {
                var clientId = pathParts[2];
                var client = _clientManager.GetAllClients().FirstOrDefault(c => c.Id == clientId);
                if (client == null)
                {
                    response.StatusCode = 404; // Not Found
                    return;
                }

                try
                {
                    using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                    var body = await reader.ReadToEndAsync();
                    var parsedBody = HttpUtility.ParseQueryString(body);

                    UpdateClientSettings(client, parsedBody);

                    await _clientSettingsService.UpdateSettingsAsync(clientId, client.Frequency, client.Volume);
                    response.StatusCode = 200; // OK
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error in PUT request handling: {e.Message}");
                    response.StatusCode = 500; // Internal Server Error
                }
            }
            else
            {
                response.StatusCode = 400; // Bad Request
            }
        }

        private void UpdateClientSettings(Client client, System.Collections.Specialized.NameValueCollection parsedBody)
        {
            if (double.TryParse(parsedBody["frequency"], out var frequency))
            {
                client.Frequency = frequency;
            }

            if (int.TryParse(parsedBody["volume"], out var volume))
            {
                client.Volume = volume;
            }

            if (bool.TryParse(parsedBody["onoff"], out var onoff))
            {
                client.OnOff = onoff;
            }
        }

        private async Task HandleGetRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            var pathParts = request.Url.AbsolutePath.Split('/');
            if (pathParts.Length == 4 && pathParts[3] == "settings")
            {
                var clientId = pathParts[2];
                var settings = await _clientSettingsService.GetSettingsAsync(clientId);

                if (settings != null)
                {
                    var responseString = $"{{\"frequency\":{settings.Frequency},\"volume\":{settings.Volume}}}";
                    var buffer = Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    response.ContentType = "application/json";
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                }
                else
                {
                    response.StatusCode = 404; // Not Found
                }
            }
            else
            {
                response.StatusCode = 400; // Bad Request
            }
        }
    }
}