using System.Net;
using System.Text;
using System.Web;
using System.Text.Json;
using server.Interface;
using server.ClientHandler.ClientDatabase;
using server.ClientHandler.ChannelDatabase;
using server.ClientHandler.VolumeDatabase;
using server.ClientHandler.FrequencyDatabase;
using server.Const;

namespace server.Classes.ClientHandler
{
    public class HttpRequestHandler
    {
        private readonly IClientManager _clientManager;
        private readonly AccountService _accountService;
        private readonly ChannelService _channelService;
        private readonly VolumeService _volumeService;
        private readonly FrequencyService _frequencyService;

        public HttpRequestHandler(IClientManager clientManager, 
                                  AccountService accountService, ChannelService channelService, 
                                  VolumeService volumeService, FrequencyService frequencyService)
        {
            _clientManager = clientManager;
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

            var client = _clientManager.GetAllClients().FirstOrDefault(c => c.Id == clientId);
            if (client == null)
            {
                response.StatusCode = 404; // Not Found
                await SendJsonResponse(response, new { message = Constants.ClientNotFound });
                return;
            }

            if (settings["frequency"] != null)
            {
                try
                {
                    var frequency = double.Parse(settings["frequency"]);
                    client.Frequency = frequency;
                    await _channelService.UpdateChannelInfo(clientId, 1, frequency);
                }
                catch (Exception  ex)
                {
                    Console.WriteLine(Constants.ErrorInFrequency + $": {ex}");
                    throw;
                }
            }
            if (settings["channel"] != null)
            {
                try
                {
                    var channel = double.Parse(settings["channel"]);
                    client.Channel = (int)channel;
                    var frequency = double.Parse(settings["frequency"]);
                    client.Frequency = frequency;
                    await _channelService.UpdateChannelInfo(clientId, (int)channel, frequency);
                }
                catch (Exception  ex)
                {
                    Console.WriteLine(Constants.ErrorInFrequency + $": {ex}");
                    throw;
                }
                
            }

            if (settings["volume"] != null)
            {

                try
                {
                    var volume = int.Parse(settings["volume"]);
                    client.Volume = volume;
                    await _volumeService.UpdateVolume(clientId, volume);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(Constants.ErrorInVolume + $": {ex}");
                    throw;
                }
            }

            if (settings["onoff"] != null)
            {
                try
                {
                    var onOff = bool.Parse(settings["onoff"]);
                    client.OnOff = onOff;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(Constants.ErrorInOnOff + $" {ex}");
                    throw;
                }
            }

            response.StatusCode = 200; // OK
            await SendJsonResponse(response, new { message = Constants.SettingsUpdated });
        }
        private async Task HandleLoginRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            var body = await reader.ReadToEndAsync();
            Console.WriteLine(Constants.RecivedLoginBody, body);

            var clientModel = JsonSerializer.Deserialize<ClientModel>(body);

            if (clientModel == null || string.IsNullOrEmpty(clientModel.ClientID) || string.IsNullOrEmpty(clientModel.Password))
            {
                response.StatusCode = 400; // Bad Request
                await SendJsonResponse(response, new { message = Constants.InvalidLogin });
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
                    clientId = account.ClientID,
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
                await SendJsonResponse(response, new { message = Constants.PassUserIncorrect});
            }
        }

        private async Task HandleRegistrationRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            var body = await reader.ReadToEndAsync();
            Console.WriteLine(Constants.RecivedRegisterBody, body);

            var clientModel = JsonSerializer.Deserialize<ClientModel>(body);

            if (clientModel == null || string.IsNullOrEmpty(clientModel.ClientID) || 
                string.IsNullOrEmpty(clientModel.Password) || !Enum.IsDefined(typeof(ClientType), clientModel.Type))
            {
                response.StatusCode = 400; // Bad Request
                await SendJsonResponse(response, new { message = Constants.ErrorRegister });
                return;
            }

            var existingAccount = await _accountService.GetAccount(clientModel.ClientID);
            if (existingAccount != null)
            {
                response.StatusCode = 409; // Conflict
                await SendJsonResponse(response, new { message = Constants.PersonalNumExist });
                return;
            }

            // new account
            await _accountService.CreateAccount(clientModel);

            //frequency range for client type
            var frequencyRange = await _frequencyService.GetFrequencyRange(clientModel.Type);
            if (frequencyRange == null)
            {
                response.StatusCode = 400; // Bad Request
                await SendJsonResponse(response, new { message = Constants.ClientTypeErr });
                return;
            }

            //default channel info
            await _channelService.AddChannelInfo(clientModel.ClientID, 1, frequencyRange.MinFrequency);

            //default volume
            await _volumeService.AddVolume(clientModel.ClientID, 50);

            response.StatusCode = 201; // Created
            await SendJsonResponse(response, new { message = Constants.RegisterSuccess });
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