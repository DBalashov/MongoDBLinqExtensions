using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDBLinqExtensions.Join;

public static class JoinExtenders
{
    /// <summary>
    /// <code>
    /// [Table("requests")]
    /// public class DBRequest
    /// {
    ///     [BsonId]
    ///     public ObjectId Id           { get; set; }
    ///     public ObjectId UserId       { get; set; }
    ///     public DBRequestItem Offer   { get; set; }
    ///     public DBRequestItem Request { get; set; }
    ///
    ///     public bool Active           { get; set; }
    /// }
    ///
    /// public class DBRequestWithUserBank : DBRequest
    /// {
    ///     [LocalKey("UserId")]          public DBUser User        { get; set; }
    ///     [LocalKey("Request.BankId")]  public DBBank RequestBank { get; set; }
    ///     [LocalKey("Offer.BankId")]    public DBBank OfferBank   { get; set; }
    ///     [ForeignKey("RequestId")]     public DBBid[] Bids       { get; set; }
    /// }
    ///
    /// var items = db.Requests
    ///   .Aggregate()
    ///   .Match(p => p.Active)
    ///   .Join&lt;DBRequest, DBRequestWithUserBank&gt;(p => p.User,        // single item by Id
    ///                                           p => p.RequestBank, // single item by Id from inner document
    ///                                           p => p.OfferBank,   // single item by Id from inner document
    ///                                           p => p.Bids)        // many items - referenced by field from external document
    ///   .As&lt;DBRequestWithUserBank&gt;()
    ///   .ToListAsync();
    /// </code>
    /// </summary>
    /// <param name="source"></param>
    /// <param name="targetProperty"></param>
    /// <typeparam name="SRC">can be same RESULT</typeparam>
    /// <typeparam name="RESULT">can be same SRC</typeparam>
    /// <exception cref="NotSupportedException"></exception>
    public static IAggregateFluent<SRC> Join<SRC, RESULT>(this IAggregateFluent<SRC> source, params Expression<Func<RESULT, object>>[] targetProperty)
    {
        var result = source;
        foreach (var expression in targetProperty)
            result = result.joinSingle(expression);
        return result;
    }

    static IAggregateFluent<SRC> joinSingle<SRC, RESULT>(this IAggregateFluent<SRC> source, Expression<Func<RESULT, object>> targetProperty)
    {
        if (targetProperty.Body is not MemberExpression memberExpression)
            throw new NotSupportedException(string.Format(Consts.NOT_SUPPORTED_EXPR, targetProperty));

        var mti = memberExpression.Member.ReflectedType!.FromCache();
        if (mti.LocalKeys.TryGetValue(memberExpression.Member.Name, out var localKey))
            return asLocalKey(source, memberExpression, localKey);

        if (mti.ForeignKeys.TryGetValue(memberExpression.Member.Name, out var foreignKey))
            return asForeignKey(source, memberExpression, foreignKey);

        throw new NotSupportedException(string.Format(Consts.CANT_FIND_ATTR_ONPROP, "ForeignKey/LocalKey", memberExpression.Member.Name));
    }

    #region asForeignKey<SRC> / asLocalKey<SRC>

    static IAggregateFluent<SRC> asForeignKey<SRC>(IAggregateFluent<SRC> source, MemberExpression memberExpression, string foreignKeyName)
    {
        if (memberExpression.Type.HasElementType) // many items ([])
        {
            var mti  = memberExpression.Type.GetElementType()!.FromCache();
            var bdoc = getLookup(mti.CollectionName, Consts.DEF_PK, foreignKeyName, memberExpression.Member.Name);
            return source.AppendStage(new BsonDocumentPipelineStageDefinition<SRC, SRC>(bdoc));
        }

        // single item
        var collectionName = memberExpression.Type.FromCache().CollectionName;
        return source.AppendStage(new BsonDocumentPipelineStageDefinition<SRC, SRC>(getLookup(collectionName, Consts.DEF_PK, foreignKeyName, memberExpression.Member.Name)))
                     .AppendStage(new BsonDocumentPipelineStageDefinition<SRC, SRC>(getUnwind(memberExpression.Member.Name)));
    }

    static IAggregateFluent<SRC> asLocalKey<SRC>(IAggregateFluent<SRC> source, MemberExpression memberExpression, string localKeyName)
    {
        if (memberExpression.Type.HasElementType) // many items ([])
        {
            var mti  = memberExpression.Type.GetElementType()!.FromCache();
            var bdoc = getLookup(mti.CollectionName, localKeyName, Consts.DEF_PK, memberExpression.Member.Name);
            return source.AppendStage(new BsonDocumentPipelineStageDefinition<SRC, SRC>(bdoc));
        }

        // single item
        var collectionName = memberExpression.Type.FromCache().CollectionName;
        return source.AppendStage(new BsonDocumentPipelineStageDefinition<SRC, SRC>(getLookup(collectionName, localKeyName, Consts.DEF_PK, memberExpression.Member.Name)))
                     .AppendStage(new BsonDocumentPipelineStageDefinition<SRC, SRC>(getUnwind(memberExpression.Member.Name)));
    }

    #endregion

    #region pipeline bsonDocuments

    static BsonDocument getLookup(string tableName, string localField, string foreignKeyField, string memberName) =>
        new BsonDocument().Add("$lookup", new BsonDocument().Add("from", tableName)
                                                            .Add("localField",   localField)
                                                            .Add("foreignField", foreignKeyField)
                                                            .Add("as",           memberName));

    static BsonDocument getUnwind(string memberName) =>
        new BsonDocument().Add("$unwind", new BsonDocument()
                                         .Add("path",                       "$" + memberName)
                                         .Add("preserveNullAndEmptyArrays", true));

    #endregion
}