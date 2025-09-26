using Redis.OM.Modeling;

[Document(StorageType = StorageType.Json, Prefixes = new []{"genjob:company"})]
public class CompanyModel
{
    [RedisIdField] 
    public string Id { get; set; }

    [Searchable]
    public List<string> company { get; set; }
}