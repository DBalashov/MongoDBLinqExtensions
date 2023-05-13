using MongoDB.Driver;

namespace MongoDBLinqExtensions.Tests;

public abstract class Base
{
    public const string databaseName = "mongodblinqextensions-join-tests";
    
    public readonly IMongoDatabase db = new MongoClient("mongodb://localhost:27017").GetDatabase(databaseName);
}