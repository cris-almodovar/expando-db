using ExpandoDB.Search;
using ExpandoDB.Storage;

namespace ExpandoDB
{
    /// <summary>
    /// Represents a collection of Content objects.
    /// </summary>
    /// <remarks>
    /// This class is analogous to an RDBMS table.
    /// </remarks>
    public class Collection
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public IndexSchema IndexSchema { get; set; }
        public IContentStorage Storage { get; set; }
        public LuceneIndex FullTextIndex { get; set; }

    }
}
