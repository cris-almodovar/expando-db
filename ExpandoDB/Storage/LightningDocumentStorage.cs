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
    public class LightningDocumentStorage : IDocumentStorage
    {
        private readonly string _collectionName;
        private readonly LightningStorageEngine _storageEngine;

        public LightningDocumentStorage(string collectionName, LightningStorageEngine storageEngine)
        {
            _collectionName = collectionName;
            _storageEngine = storageEngine;
        }                    

        public async Task<Guid> InsertAsync(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            
            if (document._id == null || document._id.Value == Guid.Empty)
                document._id = Guid.NewGuid();

            document._createdTimestamp = document._modifiedTimestamp = DateTime.UtcNow;
            document.ConvertDatesToUtc();

            var kv = document.ToKeyValue();
            await _storageEngine.InsertAsync(_collectionName, kv).ConfigureAwait(false);
            return document._id.Value;            
        }

        public async Task<Document> GetAsync(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("guid cannot be empty");

            var key = guid.ToByteArray();
            var kv = await _storageEngine.GetAsync(_collectionName, key).ConfigureAwait(false);

            if (kv == null)
                return null;

            return kv.ToDocument();
        }

        public async Task<IEnumerable<Document>> GetAsync(IList<Guid> guids)
        {
            if (guids == null)
                throw new ArgumentNullException(nameof(guids));

            var keys = guids.Select(g => g.ToByteArray()).ToList();
            var keyValuePairs = await _storageEngine.GetAsync(_collectionName, keys).ConfigureAwait(false);

            return keyValuePairs.ToEnumerableDocuments();
        }

        public Task<int> UpdateAsync(Document document)
        {
            throw new NotImplementedException();
        }        

        public Task<int> DeleteAsync(Guid guid)
        {
            throw new NotImplementedException();
        }

        public Task<int> DeleteAsync(IList<Guid> guids)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExistsAsync(Guid guid)
        {
            throw new NotImplementedException();
        }

        public Task DropAsync()
        {
            throw new NotImplementedException();
        }        
    }
}
