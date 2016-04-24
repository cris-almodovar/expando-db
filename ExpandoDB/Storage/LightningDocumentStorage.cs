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

        public Task<Document> GetAsync(Guid guid)
        {
            throw new NotImplementedException();
        }

        public Task<Guid> InsertAsync(Document document)
        {
            throw new NotImplementedException();
        }

        public Task<int> UpdateAsync(Document document)
        {
            throw new NotImplementedException();
        }
    }


}
