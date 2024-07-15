using NAudio.Wave;

namespace Client.Classes;

public class FullAudioMaker
{ 
    static WaveInEvent waveSource = null;
    static WaveFileWriter waveFile = null;
    static string outputFilePath = "recorded_audio.wav";
    static bool isRecording = false;
    

    public void StartRecording()
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
        isRecording = true;
        Console.WriteLine("Recording started. Press 'N' to stop.");
    }

    public void StopRecording()
    {
        waveSource.StopRecording();
        waveSource.Dispose();
        waveSource = null;
        waveFile.Dispose();
        waveFile = null;
        isRecording = false;
        Console.WriteLine("Recording stopped.");
    }

    public void PlayRecording()
    {
        Console.WriteLine("Playing recorded audio...");
        using (var audioFile = new AudioFileReader(outputFilePath))
        using (var outputDevice = new WaveOutEvent())
        {
            outputDevice.Init(audioFile);
            outputDevice.Play();
            while (outputDevice.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(100);
            }
        }
        Console.WriteLine("Playback finished.");
    }
}