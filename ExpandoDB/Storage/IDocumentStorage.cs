using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExpandoDB.Storage
{
    /// <summary>
    /// Defines the operations that must be implemented by a Document Storage engine.
    /// </summary>
    public interface IDocumentStorage
    {
        /// <summary>
        /// Inserts a Document object into the storage.
        /// </summary>
        /// <param name="document">The Document object to insert.</param>
        /// <returns>The GUID of the inserted Document</returns>
        /// <remarks>
        /// If the Document object does not have an id, it will be auto-generated.</remarks>     
        Task<Guid> InsertAsync(Document document);

        /// <summary>
        /// Gets the Document identified by the specified GUID.
        /// </summary>
        /// <param name="guid">The GUID for the document.</param>
        /// <returns>The Document object that corresponds to the specified GUID,
        /// or null if there is no Document object with the specified GUID.</returns>
        Task<Document> GetAsync(Guid guid);

        /// <summary>
        /// Gets the Documents identified by the specified list of GUIDs.
        /// </summary>
        /// <param name="guids">A list of GUIDs identifying the Documents to be retrieved.</param>
        /// <returns></returns>
        Task<IEnumerable<Document>> GetAsync(IList<Guid> guids);

        /// <summary>
        /// Updates the Document object.
        /// </summary>
        /// <param name="document">The Document object to update.</param>
        /// <returns></returns>  
        Task<int> UpdateAsync(Document document);

        /// <summary>
        /// Deletes the Document object identified by the specified GUID.
        /// </summary>
        /// <param name="guid">The GUID of the Document to be deleted.</param>
        /// <returns></returns>
        Task<int> DeleteAsync(Guid guid);

        /// <summary>
        /// Deletes multiple Documents identified by the specified list of GUIDs.
        /// </summary>
        /// <param name="guids">The GUIDs of the Documents to be deleted.</param>
        /// <returns></returns>  
        Task<int> DeleteAsync(IList<Guid> guids);

        /// <summary>
        /// Checks whether a Document with the specified GUID exists.
        /// </summary>
        /// <param name="guid">The GUID of the Document.</param>
        /// <returns></returns>   
        Task<bool> ExistsAsync(Guid guid);

        /// <summary>
        /// Drops the underlying table that stores the data for this Storage.
        /// </summary>
        /// <returns></returns>
        Task DropAsync();
    }
}
