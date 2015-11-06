using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExpandoDB.Storage
{
    /// <summary>
    /// Provides persistence functions for dynamic Content.
    /// </summary>
    public interface IContentStorage
    {        
        Task<Guid> InsertAsync(Content content);
        Task<Content> GetAsync(Guid guid);
        Task<IEnumerable<Content>> GetAsync(IList<Guid> guids);
        Task<int> UpdateAsync(Content content);
        Task<int> DeleteAsync(Guid guid);
        Task<int> DeleteAsync(IList<Guid> guids);
        Task<bool> ExistsAsync(Guid guid);
    }
}
