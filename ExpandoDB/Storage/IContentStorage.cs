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
        /// <summary>
        /// Inserts a Content object into the storage.
        /// </summary>
        /// <param name="content">The Content object to insert.</param>
        /// <returns>The GUID of the inserted Content</returns>
        /// <remarks>
        /// If the Content object does not have an id, it will be auto-generated.</remarks>     
        Task<Guid> InsertAsync(Content content);

        /// <summary>
        /// Gets the Content identified by the specified GUID.
        /// </summary>
        /// <param name="guid">The GUID for the content.</param>
        /// <returns>The Content object that corresponds to the specified GUID,
        /// or null if there is no Content object with the specified GUID.</returns>
        Task<Content> GetAsync(Guid guid);

        /// <summary>
        /// Gets the Contents identified by the specified list of GUIDs.
        /// </summary>
        /// <param name="guids">A list of GUIDs identifying the Contents to be retrieved.</param>
        /// <returns></returns>
        Task<IEnumerable<Content>> GetAsync(IList<Guid> guids);

        /// <summary>
        /// Updates the Content object.
        /// </summary>
        /// <param name="content">The Content object to update.</param>
        /// <returns></returns>  
        Task<int> UpdateAsync(Content content);

        /// <summary>
        /// Deletes the Content object identified by the specified GUID.
        /// </summary>
        /// <param name="guid">The GUID of the Content to be deleted.</param>
        /// <returns></returns>
        Task<int> DeleteAsync(Guid guid);

        /// <summary>
        /// Deletes multiple Contents identified by the specified list of GUIDs.
        /// </summary>
        /// <param name="guids">The GUIDs of the Contents to be deleted.</param>
        /// <returns></returns>  
        Task<int> DeleteAsync(IList<Guid> guids);

        /// <summary>
        /// Checks whether a Content with the specified GUID exists.
        /// </summary>
        /// <param name="guid">The GUID of the Content.</param>
        /// <returns></returns>   
        Task<bool> ExistsAsync(Guid guid);
    }
}
