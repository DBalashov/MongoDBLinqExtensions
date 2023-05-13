using System.ComponentModel.DataAnnotations.Schema;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDBLinqExtensions.Hierarchy.Tests;

[Table("group")]
public class DBGroup
{
    [BsonId]
    public ObjectId Id { get; set; }
    
    [ParentId]
    public ObjectId ParentId { get; set; }
    
    public string Name { get; set; }
    
    public int Level { get; set; }
}