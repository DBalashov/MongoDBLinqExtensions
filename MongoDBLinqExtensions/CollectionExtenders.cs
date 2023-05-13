using MongoDB.Driver;

namespace MongoDBLinqExtensions;

public static class CollectionExtenders
{
    public static IMongoCollection<T> GetCollectionByType<T>(this IMongoDatabase db) =>
        db.GetCollection<T>(typeof(T).GetCollectionName());

    public static string GetCollectionName(this Type type) => type.FromCache().CollectionName;
}