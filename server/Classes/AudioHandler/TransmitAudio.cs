using System.Net.WebSockets;
using server.Classes.ClientHandler;
using server.Const;
using server.Interface;

public class TransmitAudio : ITransmitAudio
{
    private readonly IClientManager _clientManager;

    public TransmitAudio(IClientManager clientManager)
    {
        _clientManager = clientManager;
    }

    public async Task BroadcastAudioAsync(Client sender, byte[] audioData, int length)
    {
        var centerFrequency = sender.Frequency;
        var lowerFrequencyBound = centerFrequency - Constants.Bandwith / 2;
        var upperFrequencyBound = centerFrequency + Constants.Bandwith / 2;

        var clientsOnSameChannel = _clientManager.GetAllClients()
            .Where(client => client.Id != sender.Id &&
                             client.Frequency >= lowerFrequencyBound &&
                             client.Frequency <= upperFrequencyBound && client.OnOff);

        var broadcastTasks = clientsOnSameChannel
            .Select(client => SendAudioToClientAsync(client, audioData, length));

        await Task.WhenAll(broadcastTasks);
    }

    public async Task SendAudioToClientAsync(Client client, byte[] audioData, int length)
    {
        try
        {
            byte[] header = new byte[] { 0xAA, 0xAA, 0xAA, 0xAA };
            byte[] sampleRateBytes = BitConverter.GetBytes(Constants.SampleRate);
            byte[] messageToSend = new byte[header.Length + sampleRateBytes.Length + length];

            Buffer.BlockCopy(header, 0, messageToSend, 0, header.Length);
            Buffer.BlockCopy(sampleRateBytes, 0, messageToSend, header.Length, sampleRateBytes.Length);
            Buffer.BlockCopy(audioData, 0, messageToSend, header.Length + sampleRateBytes.Length, length);
            
            byte[] adjustedAudioData = AdjustVolume(audioData, client.Volume);
            Buffer.BlockCopy(adjustedAudioData, 0, messageToSend, header.Length + sampleRateBytes.Length, length);


            if (client.WebSocket.State == WebSocketState.Open)
            {
                await client.WebSocket.SendAsync(new ArraySegment<byte>(messageToSend), WebSocketMessageType.Binary, true, CancellationToken.None);
                Console.WriteLine($"Sent audio to client {client.Id}, length: {messageToSend.Length} bytes");            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(Constants.ErrorSendingAudioToClient,client.Id,ex.Message);
        }
    }
    private byte[] AdjustVolume(byte[] audioData, int volume)
    {
        byte[] adjustedData = new byte[audioData.Length];
        float volumeFactor = volume / 100f;

        for (int i = 0; i < audioData.Length; i += 2)
        {
            short sample = BitConverter.ToInt16(audioData, i);
            float adjustedSample = sample * volumeFactor;
            short clippedSample = (short)Math.Clamp(adjustedSample, short.MinValue, short.MaxValue);
            byte[] adjustedBytes = BitConverter.GetBytes(clippedSample);
            adjustedData[i] = adjustedBytes[0];
            adjustedData[i + 1] = adjustedBytes[1];
        }
        return adjustedData;
    }
}