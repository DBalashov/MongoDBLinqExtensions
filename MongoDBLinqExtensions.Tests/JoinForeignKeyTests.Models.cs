using System.ComponentModel.DataAnnotations.Schema;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDBLinqExtensions.Join;

#pragma warning disable CS8618

namespace MongoDBLinqExtensions.ForeignKey.Tests;

[Table("request-fk")]
public class DBRequest
{
    [BsonId]
    public ObjectId Id { get; set; }
    
    public string Name { get; set; }
}

public class DBRequestJoined : DBRequest
{
    // single item
    [ForeignKey("RequestId")]
    public DBUser User { get; set; }

    // multiple items
    [ForeignKey("RequestId")]
    public DBRequestDetail[] RequestDetails { get; set; }
}

[Table("request-fk-detail")]
public class DBRequestDetail
{
    [BsonId]
    public ObjectId Id { get; set; }

    public ObjectId RequestId { get; set; }

    public string Name { get; set; }
}

[Table("user-fk")]
public class DBUser
{
    [BsonId]
    public ObjectId Id { get; set; }

    public ObjectId RequestId { get; set; }

    public string Login { get; set; }
}