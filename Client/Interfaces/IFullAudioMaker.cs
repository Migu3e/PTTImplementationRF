namespace Client.Interfaces;

public interface IFullAudioMaker
{
    void StartRecording();
    void StopRecording();
    byte[] GetFullAudioData();
}