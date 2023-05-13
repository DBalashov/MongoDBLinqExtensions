using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDBLinqExtensions;

public static class SaveOneExtenders
{
    public static async Task<T> SaveOneAsync<T>(this IMongoCollection<T> collection, T item) where T : class
    {
        var mti = typeof(T).FromCache();

        var id = (ObjectId) mti.PrimaryKey.GetValue(item)!;
        if (id == ObjectId.Empty)
        {
            mti.PrimaryKey.SetValue(item, ObjectId.GenerateNewId());
            await collection.InsertOneAsync(item);
        }
        else
        {
            await collection.ReplaceOneAsync(Builders<T>.Filter.Eq("_id", id), item);
        }

        return item;
    }

    public static T SaveOne<T>(this IMongoCollection<T> collection, T item) where T : class
    {
        var mti = typeof(T).FromCache();

        var id = (ObjectId) mti.PrimaryKey.GetValue(item)!;
        if (id == ObjectId.Empty)
        {
            mti.PrimaryKey.SetValue(item, ObjectId.GenerateNewId());
            collection.InsertOne(item);
        }
        else
        {
            collection.ReplaceOneAsync(Builders<T>.Filter.Eq("_id", id), item);
        }

        return item;
    }
}