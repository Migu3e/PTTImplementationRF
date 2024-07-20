namespace Client.Const;

public static class Constants
{
    public const string ServerIP = "localhost";
    public const int ServerPort = 8080;
    public const string ErrorMessage = "Error: ";
    public const string ConnectedMessage = "Connected to TCP server";
    public const string FailedToConnectMessage = "Failed to connect to TCP server: ";
    public const string PressMessage = "Press 'T' to start/stop transmission and recording, 'Q' to quit.";
    public const string TransmissionStartedMessage = "Transmission and recording started. Press 'T' to stop.";
    public const string TransmissionStoppedMessage = "Transmission and recording stopped. Full audio sent to server.";
    public const string DisconnectedMessage = "Disconnected from TCP server";
    public const string ProgramExitedMessage = "Program exited.";
    public const string NoAudioDataMessage = "No audio data to send.";
}

