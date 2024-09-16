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
    public const string ClientInfoMessage = "- ID: {0}, Status: {1}, Frequency: {2}";
    public const string ClientDisconnectReason = "Client disconnected";
    
    //websocketcontroller
    //some will be deleted when debug is not needed
    public const string ShortMessageError = "Received message is too short: {0} bytes";
    public const string ErrorInFrequency = "Error when getting Frequency or Channel.";
    public const string ErrorInVolume = "Error when getting Volume.";
    public const string ErrorInOnOff = "Error when getting On or Off settings.";
    
    //gridfs
    public const string SavedFullAudio = "Audio file saved to MongoDB: {0}, File ID: {1}";
    public const string ErrorSavingFullAudio = "error saving full audio";

    
    //HTTPreqHandler
    public const string RecivedLoginBody = "Recived Login Body: {0}";
    public const string RecivedRegisterBody = "Recived Register Body: {0}";
    public const string ClientNotFound = "Client Not Found";
    public const string SettingsUpdated = "Settings Updated Successfully";
    public const string InvalidLogin = "Invalid Login Data";
    public const string PassUserIncorrect = "Password or Username Are Incorrect";
    public const string ErrorRegister = "Error In The Registration";
    public const string PersonalNumExist = "Personal Number Already In Exist";
    public const string ClientTypeErr = "Invalid Client Type";
    public const string RegisterSuccess = "Registration Success";

    //program
    public const string StartedConnection = "WebSocket Server started. Press 'Q' to quit or 'L' to list connected clients.";
    public const string StoppedConnection = "WebSocket Server stopped";
    public const string WebServerStartedOn = "Starting WebSocket server on LAN... ";
    public const string ServerConnectionPoint = "Server is running. LAN clients can connect to:";
    
    //others
    public const int SampleRate = 44100;
    public const double Bandwith = 0.025;

}