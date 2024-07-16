using NAudio.Wave;
using System;
using System.IO;

namespace Client.Classes
{
    public class FullAudioMaker
    { 
        private WaveInEvent waveSource = null;
        private WaveFileWriter waveFile = null;
        private string outputFilePath;

        public void StartRecording()
        {
            // Generate a unique filename for this recording session
            outputFilePath = $"recorded_audio_{DateTime.Now:yyyyMMddHHmmss}.wav";

            try
            {
                waveSource = new WaveInEvent();
                waveSource.WaveFormat = new WaveFormat(44100, 1);

                waveSource.DataAvailable += (sender, e) =>
                {
                    if (waveFile != null)
                    {
                        waveFile.Write(e.Buffer, 0, e.BytesRecorded);
                        waveFile.Flush();
                    }
                };

                waveFile = new WaveFileWriter(outputFilePath, waveSource.WaveFormat);

                waveSource.StartRecording();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting recording: {ex.Message}");
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
                    return File.ReadAllBytes(outputFilePath);
                    File.Delete(outputFilePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading audio file: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("No audio file found. Please record audio first.");
            }
            return new byte[0];
        }
    }
}