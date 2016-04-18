namespace ExpandoDB.Storage
{
    /// <summary>
    /// Represents a row from the Document Storage engine.
    /// </summary>
    public class StorageRow
    {
        public string id { get; set; }
        public string json { get; set; }
    }
}
