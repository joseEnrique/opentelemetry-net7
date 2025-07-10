using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ServiceB.Models;

public class Item
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
} 