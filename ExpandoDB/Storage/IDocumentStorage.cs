using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExpandoDB.Storage
{
    /// <summary>
    /// Defines the operations that must be implemented by a Document Storage engine.
    /// </summary>
    public interface IDocumentStorage : IDisposable
    {
        /// <summary>
        /// Inserts a Document into the collection.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="document">The Document object to insert.</param>
        /// <returns>
        /// The GUID of the inserted Document
        /// </returns>
        /// <remarks>
        /// If the Document object does not have an id, it will be auto-generated.
        /// </remarks>
        Task<Guid> InsertAsync(string collectionName, Document document);

        /// <summary>
        /// Gets the Document identified by the specified GUID, from the collection.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="guid">The GUID for the document.</param>
        /// <returns>
        /// The Document object that corresponds to the specified GUID,
        /// or null if there is no Document object with the specified GUID.
        /// </returns>
        Task<Document> GetAsync(string collectionName, Guid guid);

        /// <summary>
        /// Gets the Documents identified by the specified list of GUIDs.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="guids">A list of GUIDs identifying the Documents to be retrieved.</param>
        /// <returns></returns>
        Task<IEnumerable<Document>> GetAsync(string collectionName, IList<Guid> guids);


        /// <summary>
        /// Gets all Documents from the collection.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <returns></returns>
        Task<IEnumerable<Document>> GetAllAsync(string collectionName);

        /// <summary>
        /// Updates the Document contained in the collection.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="document">The Document object to update.</param>
        /// <returns></returns>
        Task<int> UpdateAsync(string collectionName, Document document);

        /// <summary>
        /// Deletes the Document identified by the specified GUID.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="guid">The GUID of the Document to be deleted.</param>
        /// <returns></returns>
        Task<int> DeleteAsync(string collectionName, Guid guid);

        /// <summary>
        /// Deletes multiple Documents identified by the specified list of GUIDs.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="guids">The GUIDs of the Documents to be deleted.</param>
        /// <returns></returns>
        Task<int> DeleteAsync(string collectionName, IList<Guid> guids);

        /// <summary>
        /// Checks whether a Document with the specified GUID exists.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="guid">The GUID of the Document.</param>
        /// <returns></returns>
        Task<bool> ExistsAsync(string collectionName, Guid guid);

        /// <summary>
        /// Drops the underlying table that stores the data for the collection.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <returns></returns>
        Task DropAsync(string collectionName);
    }
}
