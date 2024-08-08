namespace server.Const;

public static class Constants
{
    public const string MongoConnectionString = "mongodb://localhost:27017/?directConnection=true";
    public const string DatabaseName = "PTTAudioDB";

    public const string FolderToSave = "AudioFiles";
    public const int WebSocketPort = 8081;
    public const int ServerPort = 8080;
    public const string ServerStartedOnPort = "TCP Server started on port";
    public const string QuitCommand = "Q";
    public const string ListClientsCommand = "L";
    public const string ServerStartingMessage = "Starting TCP Server...";
    public const string ServerStoppedMessage = "Server stopped. Press any key to exit.";
    public const string UnrecognizedKeyMessage = "Unrecognized key: {0}. Press '{1}' to quit or '{2}' to list clients.";
    public const string ClientConnectedMessage = "Client connected. Assigned ID: {0}";
    public const string ClientDisconnectedMessage = "Client disconnected. ID: {0}";
    public const string ConnectedClientsMessage = "Connected clients ({0}):";
    public const string ClientInfoFormat = "- ID: {0}, IP: {1}, Channel: {2}";
    public const string ErrorAcceptingClientMessage = "Error accepting client: {0}";
    public const string ErrorHandlingClientMessage = "Error handling client {0}: {1}";
    public const string ReceivedFullAudioMessage = "Received full audio ({0} bytes) from client {1}";
    public const string ErrorSendingAudioMessage = "Error sending audio to client {0}: {1}";
    public const string ServerStoppedConsoleMessage = "TCP Server stopped";
}