namespace ExpandoDB.Storage
{
    /// <summary>
    /// Represents a row from the Content Storage engine.
    /// </summary>
    public class StorageRow
    {
        public string id { get; set; }
        public string json { get; set; }
    }
}
