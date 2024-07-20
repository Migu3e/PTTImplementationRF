using System.Net.Sockets;
using server.Const;
using server.Interface;

namespace server.Classes.ClientHandler
{
    public class ClientHandler : IClientHandler
    {
        private readonly IClientManager _clientManager;
        private readonly IReceiveAudio _receiveAudio;

        public ClientHandler(IClientManager clientManager, IReceiveAudio receiveAudio)
        {
            _clientManager = clientManager;
            _receiveAudio = receiveAudio;
        }

        public async Task HandleClientAsync(Client client, bool isRunning)
        {
            try
            {
                using (NetworkStream stream = client.TcpClient.GetStream())
                {
                    byte[] headerBuffer = new byte[4];
                    while (isRunning)
                    {
                        int headerBytesRead = await stream.ReadAsync(headerBuffer, 0, 4);
                        if (headerBytesRead == 4)
                        {
                            if (headerBuffer[0] == 0xAA && headerBuffer[1] == 0xAA && headerBuffer[2] == 0xAA)
                            {
                                client.Channel = headerBuffer[3];
                                await _receiveAudio.HandleRealtimeAudioAsync(client, stream);
                            }
                            else if (headerBuffer[0] == 0xFF && headerBuffer[1] == 0xFF && headerBuffer[2] == 0xFF && headerBuffer[3] == 0xFF)
                            {
                                await _receiveAudio.HandleFullAudioTransmissionAsync(client, stream);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(Constants.ErrorHandlingClientMessage, client.Id, ex.Message);
            }
            finally
            {
                _clientManager.RemoveClient(client.Id);
            }
        }
    }
}