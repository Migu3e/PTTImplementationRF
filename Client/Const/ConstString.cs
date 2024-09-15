namespace Client.Const;

public static class ConstString
{
    public const string ServerIP = "localhost";
    public const int ServerPort = 8080;
    public const string ErrorMessage = "Error: ";
    public const string ConnectedMessage = "Connected to TCP server";
    public const string FailedToConnectMessage = "Failed to connect to TCP server: ";
    public const string PressMessage = "--------------------------------------------------------------------------------\n'T' to start/stop transmission and recording.\n'C' To Switch Channel\n'Q' to quit.\n--------------------------------------------------------------------------------";
    public const string TransmissionStartedMessage = "Transmission and recording started. Press 'T' to stop.";
    public const string TransmissionStoppedMessage = "Transmission and recording stopped. Full audio sent to server.";
    public const string ExitMessage = "Exiting..";
    public const string EnterChannel = "Enter channel number to switch (1-11): ";
    public const string SwitchedChannel = "Switched to channel: ";
    public const string InvalidChannelNumber = "Invalid channel number. Please enter a number between 1 and 11.";
    public const string InvalidKeyMessage = "Invalid Key.";
    public const string DisconnectedMessage = "Disconnected from TCP server";
    public const string ProgramExitedMessage = "Program exited.";
    public const string SendAudioToServer = "Sent full audio to server";
    public const string NoAudioDataMessage = "No audio data to send.";
}

