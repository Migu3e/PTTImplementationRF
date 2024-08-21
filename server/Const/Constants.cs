namespace server.Const;

public static class Constants
{
    public const string MongoConnectionString = "mongodb://localhost:27017/?directConnection=true";
    public const string DatabaseName = "PTTAudioDB";

    public const string FolderToSave = "AudioFiles";
    public const int WebSocketServerPort = 8081;
    public const string QuitCommand = "Q";
    public const string ListClientsCommand = "L";
    public const string UnrecognizedKeyMessage = "Unrecognized key: {0}. Press '{1}' to quit or '{2}' to list clients.";
    public const string ReceivedFullAudioMessage = "Received full audio ({0} bytes) from client {1}";
    
    //serveroptions
    public const string ClientConnectedMessage = "WebSocket client connected. ID: {0}";
    public const string ClientDisconnectedMessage = "WebSocket client disconnected. ID: {0}";
    public const string ConnectedClientsListMessage = "Connected WebSocket clients ({0}):";
    public const string ClientInfoMessage = "- ID: {0}, Channel: {1}";
    public const string ClientDisconnectReason = "Client disconnected";
    public const string ServerAccessible = "Server is accessible at:";
    
    //websockethandeler
    //some will be deleted when debug is not needed
    public const string WebSocketConnectionClosed = "WebSocket connection closed.";
    public const string ReceivedMessageInfo = "Received message: Type={0}, Count={1}, EndOfMessage={2}";
    public const string ShortMessageError = "Received message is too short: {0} bytes";
    public const string IncompleteFullAudioMessage = "Full audio message is incomplete: expected {0} bytes, got {1} bytes";
    public const string ReceivedFullAudioInfo = "Received full audio: {0} bytes";
    
    //program
    public const string StartedConnection = "WebSocket Server started. Press 'Q' to quit or 'L' to list connected clients.";
    public const string StoppedConnection = "WebSocket Server stopped";
    public const string WebServerStartedOn = "Starting WebSocket server on LAN... ";
    public const string ServerConnectionPoint = "Server is running. LAN clients can connect to:";
    
    //others
    public const int SampleRate = 44100;
    public const double Bandwith = 0.025;

}