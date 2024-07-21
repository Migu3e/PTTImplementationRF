using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Client.Const;
using Client.Interfaces;

namespace Client.Classes.AudioHandler
{
    public class TransmissionManager : ITransmissionManager
    {
        private readonly ISender _sender;
        private readonly IFullAudioMaker _fullAudioMaker;
        private bool _isTransmitting;

        public TransmissionManager(ISender sender, IFullAudioMaker fullAudioMaker)
        {
            _sender = sender;
            _fullAudioMaker = fullAudioMaker;
            _isTransmitting = false;
        }


        public void StartTransmission()
        {
            if (!_isTransmitting)
            {
                _sender.Start();
                _fullAudioMaker.StartRecording();
                _isTransmitting = true;
                Console.WriteLine(ConstString.TransmissionStartedMessage);
            }
        }

        public async Task StopTransmission(NetworkStream stream)
        {
            if (_isTransmitting)
            {
                await Task.Delay(50);
                _sender.Stop();
                _fullAudioMaker.StopRecording();
                await _sender.SendFullAudioToServer(stream, _fullAudioMaker);
                _isTransmitting = false;
                Console.WriteLine(ConstString.TransmissionStoppedMessage);
            }
        }

        public async Task ToggleTransmission(NetworkStream stream)
        {
            if (_isTransmitting)
            {
                await StopTransmission(stream);
            }
            else
            {
                StartTransmission();
            }
        }

        public async Task TransmitAudio(NetworkStream stream, byte currentChannel)
        {
            if (_isTransmitting)
            {
                await _sender.TransmitAudioToServer(stream, _sender, currentChannel);
            }
        }
    }
}