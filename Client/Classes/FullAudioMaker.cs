using NAudio.Wave;
using System;
using System.IO;
using Client.Const;
using Client.Interfaces;

namespace Client.Classes
{
    public class FullAudioMaker : IFullAudioMaker
    {
        private WaveInEvent waveSource = null;
        private WaveFileWriter waveFile = null;
        private string outputFilePath;

        public void StartRecording()
        {
            outputFilePath = $"recorded_audio_{DateTime.Now:yyyyMMddHHmmss}.wav";
            try
            {
                waveSource = new WaveInEvent();
                waveSource.WaveFormat = new WaveFormat(44100, 1);
                waveSource.DataAvailable += (sender, e) =>
                {
                    waveFile?.Write(e.Buffer, 0, e.BytesRecorded);
                    waveFile?.Flush();
                };
                waveFile = new WaveFileWriter(outputFilePath, waveSource.WaveFormat);
                waveSource.StartRecording();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{Constants.ErrorMessage} {ex.Message}");
                StopRecording();
            }
        }

        public void StopRecording()
        {
            waveSource?.StopRecording();
            waveSource?.Dispose();
            waveSource = null;

            waveFile?.Dispose();
            waveFile = null;
        }

        public byte[] GetFullAudioData()
        {
            if (File.Exists(outputFilePath))
            {
                try
                {
                    byte[] data = File.ReadAllBytes(outputFilePath);
                    File.Delete(outputFilePath);
                    return data;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{Constants.ErrorMessage} {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine(Constants.NoAudioDataMessage);
            }
            return new byte[0];
        }
    }
}
