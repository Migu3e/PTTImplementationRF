using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Text.Json;
using server.Classes.ClientHandler;
using server.Interface;
using server.ClientHandler.ClientDatabase;
using server.ClientHandler.ChannelDatabase;
using server.ClientHandler.VolumeDatabase;
using server.ClientHandler.FrequencyDatabase;

namespace server.Classes.ClientHandler
{
    public class HttpRequestHandler
    {
        private readonly IClientManager _clientManager;
        private readonly ClientSettingsService _clientSettingsService;
        private readonly AccountService _accountService;
        private readonly ChannelService _channelService;
        private readonly VolumeService _volumeService;
        private readonly FrequencyService _frequencyService;

        public HttpRequestHandler(IClientManager clientManager, ClientSettingsService clientSettingsService, 
                                  AccountService accountService, ChannelService channelService, 
                                  VolumeService volumeService, FrequencyService frequencyService)
        {
            _clientManager = clientManager;
            _clientSettingsService = clientSettingsService;
            _accountService = accountService;
            _channelService = channelService;
            _volumeService = volumeService;
            _frequencyService = frequencyService;
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

                if (request.Url.AbsolutePath.StartsWith("/api/register") && request.HttpMethod == "POST")
                {
                    await HandleRegistrationRequest(request, response);
                }
                else if (request.Url.AbsolutePath == "/api/login" && request.HttpMethod == "POST")
                {
                    await HandleLoginRequest(request, response);
                }
                else if (request.Url.AbsolutePath.StartsWith("/api/client/"))
                {
                    var pathParts = request.Url.AbsolutePath.Split('/');
                    if (pathParts.Length >= 4 && pathParts[4] == "settings")
                    {
                        switch (request.HttpMethod)
                        {
                            case "GET":
                                await HandleGetSettingsRequest(request, response);
                                break;
                            case "PUT":
                                await HandleUpdateSettingsRequest(request, response);
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

        private async Task HandleGetSettingsRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            var pathParts = request.Url.AbsolutePath.Split('/');
            var clientId = pathParts[3];

            var account = await _accountService.GetAccount(clientId);
            var channelInfo = await _channelService.GetChannelInfo(clientId);
            var volume = await _volumeService.GetLastVolume(clientId);
            var frequencyRange = await _frequencyService.GetFrequencyRange(account.Type);

            if (account != null && channelInfo != null && frequencyRange != null)
            {
                var responseData = new
                {
                    clientId = account.ClientID,
                    type = account.Type,
                    channel = channelInfo.Channel,
                    frequency = channelInfo.Frequency,
                    volume = volume,
                    minFrequency = frequencyRange.MinFrequency,
                    maxFrequency = frequencyRange.MaxFrequency
                };

                await SendJsonResponse(response, responseData);
            }
            else
            {
                response.StatusCode = 404; // Not Found
            }
        }

        private async Task HandleUpdateSettingsRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            var pathParts = request.Url.AbsolutePath.Split('/');
            var clientId = pathParts[3];

            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            var body = await reader.ReadToEndAsync();
            var settings = HttpUtility.ParseQueryString(body);

            if (settings["frequency"] != null)
            {
                var frequency = double.Parse(settings["frequency"]);
                await _channelService.UpdateChannelInfo(clientId, 1, frequency); // Assuming channel 1 for simplicity
            }

            if (settings["volume"] != null)
            {
                var volume = int.Parse(settings["volume"]);
                await _volumeService.UpdateVolume(clientId, volume);
            }

            if (settings["onoff"] != null)
            {
                var onOff = bool.Parse(settings["onoff"]);
                // Implement method to update on/off state if needed
            }

            response.StatusCode = 200; // OK
            await SendJsonResponse(response, new { message = "Settings updated successfully" });
        }

        private async Task HandleLoginRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            var body = await reader.ReadToEndAsync();
            Console.WriteLine($"Received login request body: {body}");

            var clientModel = JsonSerializer.Deserialize<ClientModel>(body);

            if (clientModel == null || string.IsNullOrEmpty(clientModel.ClientID) || string.IsNullOrEmpty(clientModel.Password))
            {
                response.StatusCode = 400; // Bad Request
                await SendJsonResponse(response, new { message = "Invalid login data" });
                return;
            }

            var isValid = await _accountService.ValidateCredentials(clientModel.ClientID, clientModel.Password);
            if (isValid)
            {
                var account = await _accountService.GetAccount(clientModel.ClientID);
                var channelInfo = await _channelService.GetChannelInfo(clientModel.ClientID);
                var volume = await _volumeService.GetLastVolume(clientModel.ClientID);
                var frequencyRange = await _frequencyService.GetFrequencyRange(account.Type);

                var responseData = new
                {
                    message = "Login successful",
                    clientId = account.ClientID, // This is now the same as the login ID
                    type = account.Type,
                    channel = channelInfo?.Channel ?? 1,
                    frequency = channelInfo?.Frequency ?? 30.0000,
                    volume = volume,
                    minFrequency = frequencyRange?.MinFrequency,
                    maxFrequency = frequencyRange?.MaxFrequency
                };

                response.StatusCode = 200; // OK
                await SendJsonResponse(response, responseData);
            }
            else
            {
                response.StatusCode = 401; // Unauthorized
                await SendJsonResponse(response, new { message = "Invalid credentials" });
            }
        }

        private async Task HandleRegistrationRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            var body = await reader.ReadToEndAsync();
            var clientModel = JsonSerializer.Deserialize<ClientModel>(body);

            if (clientModel == null || string.IsNullOrEmpty(clientModel.ClientID) || 
                string.IsNullOrEmpty(clientModel.Password) || !Enum.IsDefined(typeof(ClientType), clientModel.Type))
            {
                response.StatusCode = 400; // Bad Request
                await SendJsonResponse(response, new { message = "Invalid registration data" });
                return;
            }

            var existingAccount = await _accountService.GetAccount(clientModel.ClientID);
            if (existingAccount != null)
            {
                response.StatusCode = 409; // Conflict
                await SendJsonResponse(response, new { message = "Client ID already exists" });
                return;
            }

            // Create account
            await _accountService.CreateAccount(clientModel);

            // Get frequency range for client type
            var frequencyRange = await _frequencyService.GetFrequencyRange(clientModel.Type);
            if (frequencyRange == null)
            {
                response.StatusCode = 400; // Bad Request
                await SendJsonResponse(response, new { message = "Invalid client type" });
                return;
            }

            // Create default channel info
            await _channelService.UpdateChannelInfo(clientModel.ClientID, 1, frequencyRange.MinFrequency);

            // Create default volume
            await _volumeService.UpdateVolume(clientModel.ClientID, 50);

            response.StatusCode = 201; // Created
            await SendJsonResponse(response, new { message = "Registration successful" });
        }

        private void AddCorsHeaders(HttpListenerResponse response)
        {
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Methods", "GET, PUT, POST, OPTIONS");
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
                var account = await _accountService.GetAccount(clientId);
                var channelInfo = await _channelService.GetChannelInfo(clientId);
                var volume = await _volumeService.GetLastVolume(clientId);
                var frequencyRange = await _frequencyService.GetFrequencyRange(account.Type);

                if (account != null && channelInfo != null && frequencyRange != null)
                {
                    var responseData = new
                    {
                        clientId = account.ClientID,
                        type = account.Type,
                        channel = channelInfo.Channel,
                        frequency = channelInfo.Frequency,
                        volume = volume,
                        minFrequency = frequencyRange.MinFrequency,
                        maxFrequency = frequencyRange.MaxFrequency
                    };

                    await SendJsonResponse(response, responseData);
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

        private async Task SendJsonResponse(HttpListenerResponse response, object data)
        {
            var json = JsonSerializer.Serialize(data);
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }



    }
}