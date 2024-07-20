using server.Interface;

namespace server.Classes;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

public class GridFsManager : IGridFsManager
{
    private readonly IGridFSBucket gridFS;

    public GridFsManager(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        gridFS = new GridFSBucket(database);
    }

    public async Task SaveAudioAsync(string filename, byte[] audioData)
    {
        using (var stream = new MemoryStream(audioData))
        {
            await gridFS.UploadFromStreamAsync(filename, stream);
        }
    }
}