using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using MongoDB.Bson;
using server.Interface;
using server.Const;

namespace GridFs;

public class GridFsManager : IGridFsManager
{
    private readonly IGridFSBucket gridFSBucket;
    private readonly string audioDirectory;

    public GridFsManager(IMongoDatabase database)
    {
        gridFSBucket = new GridFSBucket(database);
        audioDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.FolderToSave);
        Directory.CreateDirectory(audioDirectory);
    }

    public async Task SaveAudioAsync(string filename, byte[] audioData)
    {
        try
        {
            // Save to MongoDB
            using (var stream = new MemoryStream(audioData))
            {
                ObjectId fileId = await gridFSBucket.UploadFromStreamAsync(filename, stream);
                Console.WriteLine(Constants.SavedFullAudio,filename,fileId);
            }

            // Save to local file system
            string filePath = Path.Combine(audioDirectory, filename);
            await File.WriteAllBytesAsync(filePath, audioData);
        }
        catch (Exception ex)
        {
            Console.WriteLine(Constants.ErrorSavingFullAudio);
            throw;
        }
    }
}