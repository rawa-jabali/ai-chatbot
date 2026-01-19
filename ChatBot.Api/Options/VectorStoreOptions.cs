public class VectorStoreOptions
{
    public string Provider { get; set; } = "qdrant";
    public CollectionOptions Collection { get; set; } = new();
    public QdrantOptions Qdrant { get; set; } = new();
}