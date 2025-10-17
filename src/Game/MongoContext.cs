using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Game.Models;

namespace Game.Services
{
    public class MongoContext
{
    public IMongoCollection<EncryptedSaveDoc> Saves => Db.GetCollection<EncryptedSaveDoc>("saves");
    public IMongoCollection<ProfileDoc> Profiles => Db.GetCollection<ProfileDoc>("profiles");

    public IMongoDatabase Db { get; }

    public MongoContext(string connectionString, string dbName)
    {
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);
        settings.ConnectTimeout = TimeSpan.FromSeconds(3);
        settings.WaitQueueTimeout = TimeSpan.FromSeconds(5);

        var client = new MongoClient(settings);
        Db = client.GetDatabase(dbName);
    }
    }
}
