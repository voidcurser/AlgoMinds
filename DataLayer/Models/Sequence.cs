using MongoDB.Bson.Serialization.Attributes;

namespace DataLayer.Models;

public class Sequence
{
    [BsonId]
    public string Name { get; set; }
    public int Value { get; set; }
}
