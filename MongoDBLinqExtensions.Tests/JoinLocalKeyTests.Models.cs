using System.ComponentModel.DataAnnotations.Schema;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDBLinqExtensions.Join;

#pragma warning disable CS8618

namespace MongoDBLinqExtensions.LocalKey.Tests;

[Table("request-lk")]
public class DBRequest
{
    [BsonId]
    public ObjectId Id { get; set; }

    // single item
    public ObjectId UserId { get; set; }

    // multiple items
    public ObjectId[] RequestDetailIds { get; set; }
}

public class DBRequestJoined : DBRequest
{
    [LocalKey("UserId")]
    public DBUser User { get; set; }
    
    [LocalKey("RequestDetailIds")]
    public DBRequestDetail[] RequestDetails { get; set; }
}

[Table("request-lk-detail")]
public class DBRequestDetail
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string Name { get; set; }
}

[Table("user-lk")]
public class DBUser
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string Login { get; set; }
}