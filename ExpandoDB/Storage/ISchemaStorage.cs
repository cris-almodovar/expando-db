using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExpandoDB.Storage
{
    /// <summary>
    /// Defines the operations that must be implemented by a Schema Storage engine.
    /// </summary>
    public interface ISchemaStorage
    {
        Task<DocumentCollectionSchema> GetAsync(string schemaName);
        Task<IList<DocumentCollectionSchema>> GetAllAsync();
        Task<string> InsertAsync(DocumentCollectionSchema collectionSchema);
        Task<int> UpdateAsync(DocumentCollectionSchema collectionSchema);
        Task<int> DeleteAsync(string schemaName);
    }
}
