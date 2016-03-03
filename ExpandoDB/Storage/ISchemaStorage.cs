using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExpandoDB.Storage
{
    /// <summary>
    /// Defines the operations that must be implemented by a Schema Storage engine.
    /// </summary>
    public interface ISchemaStorage
    {
        Task<IList<ContentCollectionSchema>> GetAllAsync();
        Task<string> InsertAsync(ContentCollectionSchema collectionSchema);
        Task<int> UpdateAsync(ContentCollectionSchema collectionSchema);
        Task<int> DeleteAsync(string schemaName);
    }
}
