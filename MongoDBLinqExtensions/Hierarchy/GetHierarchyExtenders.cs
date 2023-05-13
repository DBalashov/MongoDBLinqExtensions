using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

#pragma warning disable CS8618

// ReSharper disable ClassNeverInstantiated.Local

namespace MongoDBLinqExtensions.Hierarchy;

public static partial class HierarchyExtenders
{
    /// <summary>
    /// Get all hierarchy for collection.
    /// Collection must have [Table] attribute, [BsonId] attribute on key property and [ParentID] attribute on parent reference property.
    /// Return dictionary with key - parent ID, value - full hierachy with children for this parent (root parent node NOT INCLUDED)
    /// <code>
    /// [Table("ItemGroup")]
    /// public class DBItemGroup
    /// {
    ///     [BsonId]   public ObjectId ID       { get; set; }
    ///     [ParentID] public ObjectId ParentID { get; set; }
    /// }
    ///
    /// var r = await g.Aggregate()
    ///                .Match(p => ids.Contains(p.ID))
    ///                .GetHierarchy();
    /// </code>
    /// </summary>
    /// <param name="agg"></param>
    /// <exception cref="NotSupportedException"></exception>
    public static async Task<Dictionary<ObjectId, T[]>> GetHierarchyAsync<T>(this IAggregateFluent<T> agg) =>
        (await agg.getHierarchyPipeline().ToListAsync())
       .ToDictionary(p => p.ID, p => p.Child);

    /// <summary>
    /// Get all hierarchy for collection.
    /// Collection must have [Table] attribute, [BsonId] attribute on key property and [ParentID] attribute on parent reference property.
    /// Return dictionary with key - parent ID, value - full hierachy with children for this parent (root parent node NOT INCLUDED)
    /// <code>
    /// [Table("ItemGroup")]
    /// public class DBItemGroup
    /// {
    ///     [BsonId]   public ObjectId ID       { get; set; }
    ///     [ParentID] public ObjectId ParentID { get; set; }
    /// }
    ///
    /// var r = await g.Aggregate()
    ///                .Match(p => ids.Contains(p.ID))
    ///                .GetHierarchy();
    /// </code>
    /// </summary>
    /// <param name="agg"></param>
    /// <exception cref="NotSupportedException"></exception>
    public static Dictionary<ObjectId, T[]> GetHierarchy<T>(this IAggregateFluent<T> agg) =>
        agg.getHierarchyPipeline()
           .ToList()
           .ToDictionary(p => p.ID, p => p.Child);

    #region pipeline bsonDocuments

    static IAggregateFluent<HierarchyStub<T>> getHierarchyPipeline<T>(this IAggregateFluent<T> agg) =>
        agg.AppendStage(new BsonDocumentPipelineStageDefinition<T, T>(getGraphLookupHierarchy<T>()))
           .AppendStage(new BsonDocumentPipelineStageDefinition<T, T>(new BsonDocument().Add("$project",
                                                                                             new BsonDocument()
                                                                                                .Add(Consts.DEF_PK, "$" + Consts.DEF_PK)
                                                                                                .Add("Child",       "$Child"))))
           .As<HierarchyStub<T>>();

    static BsonDocument getGraphLookupHierarchy<T>()
    {
        var mti = typeof(T).FromCache();
        return new BsonDocument().Add("$graphLookup",
                                      new BsonDocument()
                                         .Add("from",             mti.CollectionName)
                                         .Add("startWith",        "$" + Consts.DEF_PK)
                                         .Add("connectFromField", Consts.DEF_PK)
                                         .Add("connectToField",   mti.ParentId?.Name ?? throw new NotSupportedException(string.Format(Consts.CANT_FIND_ATTR, "ParentId")))
                                         .Add("as",               "Child"));
    }

    #endregion

    sealed class HierarchyStub<T>
    {
        [BsonId]
        public ObjectId ID { get; set; }

        public T[] Child { get; set; }
    }
}