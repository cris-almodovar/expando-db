using ExpandoDB.Search;
using ExpandoDB.Storage;

namespace ExpandoDB
{
    public class Collection
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public IndexSchema IndexSchema { get; set; }
        public IDynamicStorage Storage { get; set; }
        public FullTextIndex FullTextIndex { get; set; }

    }
}
