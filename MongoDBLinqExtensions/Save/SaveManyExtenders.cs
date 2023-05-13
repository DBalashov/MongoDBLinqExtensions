using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDBLinqExtensions;

public static class SaveManyExtenders
{
    public static async Task<T[]> SaveManyAsync<T>(this IMongoCollection<T> collection, IEnumerable<T> items) where T : class
    {
        var arr = items.ToArray();
        var (insert, update) = splitInsertUpdate(arr);

        if (insert.Any())
            await collection.InsertManyAsync(insert);

        if (update.Any())
            await collection.BulkWriteAsync(update, new BulkWriteOptions() {IsOrdered = false});
        return arr;
    }

    public static T[] SaveMany<T>(this IMongoCollection<T> collection, IEnumerable<T> items) where T : class
    {
        var arr = items.ToArray();
        var (insert, update) = splitInsertUpdate(arr);

        if (insert.Any())
            collection.InsertMany(insert);

        if (update.Any())
            collection.BulkWrite(update, new BulkWriteOptions() {IsOrdered = false});

        return arr;
    }

    static (List<T> insert, List<WriteModel<T>> update) splitInsertUpdate<T>(T[] items)
    {
        var mti    = typeof(T).FromCache();
        var insert = new List<T>();
        var update = new List<WriteModel<T>>();
        foreach (var item in items)
        {
            var id = (ObjectId) mti.PrimaryKey.GetValue(item)!;
            if (id == ObjectId.Empty)
            {
                mti.PrimaryKey.SetValue(item, ObjectId.GenerateNewId());
                insert.Add(item);
            }
            else
            {
                update.Add(new ReplaceOneModel<T>(Builders<T>.Filter.Eq("_id", id), item));
            }
        }

        return (insert, update);
    }
}