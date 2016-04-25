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

        public Task<int> DeleteAsync(IList<Guid> guids)
        {
            throw new NotImplementedException();
        }

        public Task<int> DeleteAsync(Guid guid)
        {
            throw new NotImplementedException();
        }

        public Task DropAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExistsAsync(Guid guid)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Document>> GetAsync(IList<Guid> guids)
        {
            throw new NotImplementedException();
        }

        public async Task<Document> GetAsync(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("guid cannot be empty");
            
            var key = Encoding.UTF8.GetBytes(guid.ToString()); 
            var kv = await _storageEngine.GetAsync(_collectionName, key).ConfigureAwait(false);
            
            if (kv == null)
                return null;

            return kv.ToDocument();            
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
            await _storageEngine.InsertAsync(_collectionName, kv);
            return document._id.Value;            
        }

        public Task<int> UpdateAsync(Document document)
        {
            throw new NotImplementedException();
        }
    }


}
