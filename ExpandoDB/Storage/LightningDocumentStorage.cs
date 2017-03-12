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
    public class LightningDocumentStorage : IDocumentStorage, IDisposable
    {        
        private readonly LightningStorageEngine _storageEngine;
        private readonly HashSet<string> _initializedDatabases = new HashSet<string>();        

        /// <summary>
        /// Initializes a new instance of the <see cref="LightningDocumentStorage" /> class.
        /// </summary>
        /// <param name="dataPath">The data path.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public LightningDocumentStorage(string dataPath)
        {
            if (String.IsNullOrWhiteSpace(dataPath))
                throw new ArgumentException(nameof(dataPath));            

            _storageEngine = new LightningStorageEngine(dataPath);    
        }

        private void EnsureCollectionIsInitialized(string collectionName)
        {
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException($"{nameof(collectionName)} cannot be null or empty");

            if (!_initializedDatabases.Contains(collectionName))
            {
                lock (_initializedDatabases)
                {
                    if (!_initializedDatabases.Contains(collectionName))
                    {
                        _storageEngine.InitializeDatabase(collectionName);
                        _initializedDatabases.Add(collectionName);
                    }
                }
            }
        }

        /// <summary>
        /// Inserts a Document object into the storage.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="document">The Document object to insert.</param>
        /// <returns>
        /// The GUID of the inserted Document
        /// </returns>
        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <remarks>
        /// If the Document object does not have an id, it will be auto-generated.
        /// </remarks>
        public async Task<Guid> InsertAsync(string collectionName, Document document)
        {
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException($"{nameof(collectionName)} cannot be null or empty");
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            if (document._id == null || document._id.Value == Guid.Empty)
                document._id = Guid.NewGuid();

            document._createdTimestamp = document._modifiedTimestamp = DateTime.UtcNow;
            document.ConvertDatesToUtc();

            EnsureCollectionIsInitialized(collectionName);

            var kv = document.ToKeyValuePair();
            await _storageEngine.InsertAsync(collectionName, kv).ConfigureAwait(false);
            return document._id.Value;
        }


        /// <summary>
        /// Gets the Document identified by the specified GUID.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="guid">The GUID for the document.</param>
        /// <returns>
        /// The Document object that corresponds to the specified GUID,
        /// or null if there is no Document object with the specified GUID.
        /// </returns>
        /// <exception cref="System.ArgumentException">guid cannot be empty</exception>
        public async Task<Document> GetAsync(string collectionName, Guid guid)
        {
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException($"{nameof(collectionName)} cannot be null or empty");
            if (guid == Guid.Empty)
                throw new ArgumentException($"{nameof(guid)} cannot be empty");

            EnsureCollectionIsInitialized(collectionName);

            var key = guid.ToByteArray();
            var kv = await _storageEngine.GetAsync(collectionName, key).ConfigureAwait(false);

            if (kv == null)
                return null;

            return kv.ToDocument();
        }

        /// <summary>
        /// Gets the Documents identified by the specified list of GUIDs.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="guids">A list of GUIDs identifying the Documents to be retrieved.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.ArgumentNullException"></exception>
        public async Task<IEnumerable<Document>> GetAsync(string collectionName, IList<Guid> guids)
        {
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException($"{nameof(collectionName)} cannot be null or empty");
            if (guids == null)
                throw new ArgumentNullException(nameof(guids));

            EnsureCollectionIsInitialized(collectionName);

            var keys = guids.Select(g => g.ToByteArray());
            var keyValuePairs = await _storageEngine.GetAsync(collectionName, keys).ConfigureAwait(false);

            return keyValuePairs.ToEnumerableDocuments();
        }


        /// <summary>
        /// Gets all Documents from the collection.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException"></exception>
        public async Task<IEnumerable<Document>> GetAllAsync(string collectionName)
        {
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException($"{nameof(collectionName)} cannot be null or empty");

            EnsureCollectionIsInitialized(collectionName);

            var keyValuePairs = await _storageEngine.GetAllAsync(collectionName).ConfigureAwait(false);
            return keyValuePairs.ToEnumerableDocuments();
        }

        /// <summary>
        /// Updates the Document object.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="document">The Document object to update.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.Exception">The document does not have an _id field</exception>
        public async Task<int> UpdateAsync(string collectionName, Document document)
        {
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException($"{nameof(collectionName)} cannot be null or empty");
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            if (document._id == null || document._id == Guid.Empty)
                throw new Exception("The document does not have an _id field");

            EnsureCollectionIsInitialized(collectionName);

            var key = document._id.Value.ToByteArray();
            var existingKv = await _storageEngine.GetAsync(collectionName, key).ConfigureAwait(false);
            if (existingKv == null)                
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
            var updatedCount = await _storageEngine.UpdateAsync(collectionName, updatedKv).ConfigureAwait(false);
            return updatedCount;
            
        }

        /// <summary>
        /// Deletes the Document object identified by the specified GUID.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="guid">The GUID of the Document to be deleted.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">
        /// </exception>
        public async Task<int> DeleteAsync(string collectionName, Guid guid)
        {
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException($"{nameof(collectionName)} cannot be null or empty");
            if (guid == Guid.Empty)
                throw new ArgumentException($"{nameof(guid)} cannot be empty");

            EnsureCollectionIsInitialized(collectionName);

            var key = guid.ToByteArray();
            var deletedCount = await _storageEngine.DeleteAsync(collectionName, key).ConfigureAwait(false);
            return deletedCount;           
        }

        /// <summary>
        /// Deletes the Documents identified by the specified list of GUIDs.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="guids">The GUIDs of the Documents to be deleted.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public async Task<int> DeleteAsync(string collectionName, IList<Guid> guids)
        {
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException($"{nameof(collectionName)} cannot be null or empty");
            if (guids == null)
                throw new ArgumentNullException(nameof(guids));

            EnsureCollectionIsInitialized(collectionName);

            var keys = guids.Select(g => g.ToByteArray());
            var deletedCount = await _storageEngine.DeleteAsync(collectionName, keys).ConfigureAwait(false);

            return deletedCount;
        }

        /// <summary>
        /// Checks whether a Document with the specified GUID exists.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="guid">The GUID of the Document.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">
        /// </exception>
        public async Task<bool> ExistsAsync(string collectionName, Guid guid)
        {
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException($"{nameof(collectionName)} cannot be null or empty");
            if (guid == Guid.Empty)
                throw new ArgumentException($"{nameof(guid)} cannot be empty");

            EnsureCollectionIsInitialized(collectionName);

            var key = guid.ToByteArray();
            var exists = await _storageEngine.ExistsAsync(collectionName, key).ConfigureAwait(false);
            return exists;            
        }

        /// <summary>
        /// Drops the underlying Lightning database that stores the data for the collection
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException"></exception>
        public async Task DropAsync(string collectionName)
        {
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException($"{nameof(collectionName)} cannot be null or empty");            

            lock (_initializedDatabases)
            {                
                _initializedDatabases.Remove(collectionName);                
            }            

            await _storageEngine.DropAsync(collectionName);
        }

        /// <summary>
        /// Truncates the collection
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException"></exception>
        public async Task TruncateAsync(string collectionName)
        {
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException($"{nameof(collectionName)} cannot be null or empty");            

            await _storageEngine.TruncateAsync(collectionName);
        }

        #region IDisposable Support
        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    _storageEngine.Dispose();
                }                

                IsDisposed = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {           
            Dispose(true);            
        }
        #endregion
    }
}
