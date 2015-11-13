using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Storage
{
    public interface ISchemaStorage
    {
        Task<IList<ContentCollectionSchema>> GetAllAsync();
        Task<string> InsertAsync(ContentCollectionSchema collectionSchema);
        Task<int> UpdateAsync(ContentCollectionSchema collectionSchema);
        Task<int> DeleteAsync(string schemaName);
    }
}
