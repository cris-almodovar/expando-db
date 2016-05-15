using Common.Logging;
using Jil;
using LightningDB;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExpandoDB.Storage
{
    /// <summary>
    /// A Document Storage engine that persists data using the LightningDB key-value store.
    /// </summary>
    /// <seealso cref="ExpandoDB.Storage.IDocumentStorage" />
    public class LightningDocumentStorage : IDocumentStorage
    {
        private readonly string _collectionName;
        private readonly LightningStorageEngine _storageEngine;

        /// <summary>
        /// Initializes a new instance of the <see cref="LightningDocumentStorage"/> class.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="storageEngine">The storage engine.</param>
        public LightningDocumentStorage(string collectionName, LightningStorageEngine storageEngine)
        {
            _collectionName = collectionName;
            _storageEngine = storageEngine;
            _storageEngine.InitializeDatabase(_collectionName);
        }

        /// <summary>
        /// Inserts a Document object into the storage.
        /// </summary>
        /// <param name="document">The Document object to insert.</param>
        /// <returns>
        /// The GUID of the inserted Document
        /// </returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <remarks>
        /// If the Document object does not have an id, it will be auto-generated.
        /// </remarks>
        public async Task<Guid> InsertAsync(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            
            if (document._id == null || document._id.Value == Guid.Empty)
                document._id = Guid.NewGuid();

            document._createdTimestamp = document._modifiedTimestamp = DateTime.UtcNow;
            document.ConvertDatesToUtc();

            var kv = document.ToKeyValuePair();
            await _storageEngine.InsertAsync(_collectionName, kv).ConfigureAwait(false);
            return document._id.Value;            
        }

        /// <summary>
        /// Gets the Document identified by the specified GUID.
        /// </summary>
        /// <param name="guid">The GUID for the document.</param>
        /// <returns>
        /// The Document object that corresponds to the specified GUID,
        /// or null if there is no Document object with the specified GUID.
        /// </returns>
        /// <exception cref="System.ArgumentException">guid cannot be empty</exception>
        public async Task<Document> GetAsync(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("guid cannot be empty");

            var key = guid.ToByteArray();
            var kv = await _storageEngine.GetAsync(_collectionName, key).ConfigureAwait(false);

            if (kv.IsEmpty)
                return null;

            return kv.ToDocument();
        }

        /// <summary>
        /// Gets the Documents identified by the specified list of GUIDs.
        /// </summary>
        /// <param name="guids">A list of GUIDs identifying the Documents to be retrieved.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public async Task<IEnumerable<Document>> GetAsync(IList<Guid> guids)
        {
            if (guids == null)
                throw new ArgumentNullException(nameof(guids));

            var keys = guids.Select(g => g.ToByteArray());
            var keyValuePairs = await _storageEngine.GetAsync(_collectionName, keys).ConfigureAwait(false);

            return keyValuePairs.ToEnumerableDocuments();
        }

        /// <summary>
        /// Updates the Document object.
        /// </summary>
        /// <param name="document">The Document object to update.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.Exception">The document does not have an _id field</exception>
        public async Task<int> UpdateAsync(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            if (document._id == null || document._id == Guid.Empty)
                throw new Exception("The document does not have an _id field");
           
            var key = document._id.Value.ToByteArray();
            var existingKv = await _storageEngine.GetAsync(_collectionName, key).ConfigureAwait(false);
            if (existingKv.IsEmpty)                
                return 0;

            // Make sure the _createdTimestamp is not overwritten
            // Copy the value from the existing Document.
            var existingDocument = existingKv.ToDocument();
            document._createdTimestamp = existingDocument._createdTimestamp;

            // Always set the _modifiedTimestamp to the current UTC date/time.
            document._modifiedTimestamp = DateTime.UtcNow;

            // Make sure all date/times are in ISO UTC format.
            document.ConvertDatesToUtc();

            var updatedKv = document.ToKeyValuePair();
            var updatedCount = await _storageEngine.UpdateAsync(_collectionName, updatedKv).ConfigureAwait(false);
            return updatedCount;
            
        }

        /// <summary>
        /// Deletes the Document object identified by the specified GUID.
        /// </summary>
        /// <param name="guid">The GUID of the Document to be deleted.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">guid cannot be empty</exception>
        public async Task<int> DeleteAsync(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("guid cannot be empty");
            
            var key = guid.ToByteArray();
            var deletedCount = await _storageEngine.DeleteAsync(_collectionName, key).ConfigureAwait(false);
            return deletedCount;           
        }

        /// <summary>
        /// Deletes the Documents identified by the specified list of GUIDs.
        /// </summary>
        /// <param name="guids">The GUIDs of the Documents to be deleted.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public async Task<int> DeleteAsync(IList<Guid> guids)
        {
            if (guids == null)
                throw new ArgumentNullException(nameof(guids));

            var keys = guids.Select(g => g.ToByteArray());
            var deletedCount = await _storageEngine.DeleteAsync(_collectionName, keys).ConfigureAwait(false);

            return deletedCount;
        }

        /// <summary>
        /// Checks whether a Document with the specified GUID exists.
        /// </summary>
        /// <param name="guid">The GUID of the Document.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">guid cannot be empty</exception>
        public async Task<bool> ExistsAsync(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("guid cannot be empty");            
            
            var key = guid.ToByteArray();
            var exists = await _storageEngine.ExistsAsync(_collectionName, key).ConfigureAwait(false);
            return exists;            
        }

        /// <summary>
        /// Drops the underlying Lightning database that stores the data for this Storage.
        /// </summary>
        /// <returns></returns>
        public async Task DropAsync()
        {
            await _storageEngine.DropAsync(_collectionName);
        }        
    }
}
