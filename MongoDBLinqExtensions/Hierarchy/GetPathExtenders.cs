using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

// ReSharper disable ClassNeverInstantiated.Local

#pragma warning disable CS8618

namespace MongoDBLinqExtensions.Hierarchy;

public static partial class HierarchyExtenders
{
    /// <summary>
    /// Get path to root for items.
    /// Collection must have [Table] attribute, [BsonId] attribute on key property and [ParentID] attribute on parent reference property.
    /// Return dictionary with key - item ID, value - collection of items from root to this item (root node and leaf item INCLUDED)
    /// <code>
    /// [Table("ItemGroup")]
    /// public class DBItemGroup
    /// {
    ///     [BsonId]   public ObjectId ID       { get; set; }
    ///     [ParentID] public ObjectId ParentID { get; set; }
    /// }
    /// var r = await g.Aggregate()
    ///                .Match(p => ids.Contains(p.ID))
    ///                .GetPath();
    /// </code>
    /// </summary>
    /// <param name="agg"></param>
    /// <exception cref="NotSupportedException"></exception>
    public static async Task<Dictionary<ObjectId, T[]>> GetPathAsync<T>(this IAggregateFluent<T> agg) =>
        (await agg.getPathPipeline().ToListAsync())
       .GroupBy(p => p.ID)
       .ToDictionary(p => p.Key, p => p.OrderByDescending(c => c.Depth).Select(c => c.Child).ToArray());

    /// <summary>
    /// Get path to root for items.
    /// Collection must have [Table] attribute, [BsonId] attribute on key property and [ParentID] attribute on parent reference property.
    /// Return dictionary with key - item ID, value - collection of items from root to this item (root node and leaf item INCLUDED)
    /// <code>
    /// [Table("ItemGroup")]
    /// public class DBItemGroup
    /// {
    ///     [BsonId]   public ObjectId ID       { get; set; }
    ///     [ParentID] public ObjectId ParentID { get; set; }
    /// }
    /// var r = await g.Aggregate()
    ///                .Match(p => ids.Contains(p.ID))
    ///                .GetPath();
    /// </code>
    /// </summary>
    /// <param name="agg"></param>
    /// <exception cref="NotSupportedException"></exception>
    public static Dictionary<ObjectId, T[]> GetPath<T>(this IAggregateFluent<T> agg) =>
        agg.getPathPipeline()
           .ToList()
           .GroupBy(p => p.ID)
           .ToDictionary(p => p.Key, p => p.OrderByDescending(c => c.Depth).Select(c => c.Child).ToArray());

    #region pipeline bsonDocuments

    static IAggregateFluent<PathStub<T>> getPathPipeline<T>(this IAggregateFluent<T> agg) =>
        agg.AppendStage(new BsonDocumentPipelineStageDefinition<T, T>(getGraphLookupPath<T>()))
           .AppendStage(new BsonDocumentPipelineStageDefinition<T, T>(new BsonDocument().Add("$unwind",
                                                                                             new BsonDocument()
                                                                                                .Add("path", "$Child"))))
           .AppendStage(new BsonDocumentPipelineStageDefinition<T, T>(new BsonDocument().Add("$project",
                                                                                             new BsonDocument()
                                                                                                .Add(Consts.DEF_PK, "$" + Consts.DEF_PK)
                                                                                                .Add("Child",       "$Child")
                                                                                                .Add("Depth",       "$Child.Depth"))))
           .AppendStage(new BsonDocumentPipelineStageDefinition<T, T>(new BsonDocument().Add("$unset", "Child.Depth")))
           .As<PathStub<T>>();

    static BsonDocument getGraphLookupPath<T>()
    {
        var mti = typeof(T).FromCache();
        return new BsonDocument().Add("$graphLookup",
                                      new BsonDocument()
                                         .Add("from",             mti.CollectionName)
                                         .Add("startWith",        "$" + Consts.DEF_PK)
                                         .Add("connectFromField", mti.ParentId?.Name ?? throw new NotSupportedException(string.Format(Consts.CANT_FIND_ATTR, "ParentId")))
                                         .Add("connectToField",   Consts.DEF_PK)
                                         .Add("as",               "Child")
                                         .Add("depthField",       "Depth"));
    }

    #endregion

    sealed class PathStub<T>
    {
        [BsonId]
        public ObjectId ID { get; set; }

        public T Child { get; set; }

        public int Depth { get; set; }
    }
}