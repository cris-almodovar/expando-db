using Nancy;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using System.Threading;
using Nancy.ModelBinding;
using ExpandoDB.Search;
using System.Diagnostics;
using ExpandoDB.Server.Web.DTO;

namespace ExpandoDB.Server.Web
{
    public class DbService : NancyModule
    {
        private readonly Database _db;
        private readonly ILog _log = LogManager.GetLogger(typeof(DbService).Name);

        public DbService(Database db) : base("/db")
        {
            _db = db;

            Post["/{collection}", true] = OnInsertContentAsync;

            Get["/{collection}", true] = OnSearchContentsAsync;
            Get["/{collection}/schema"] = OnGetCollectionSchema;
            Get["/{collection}/count"] = OnGetCount;
            Get["/{collection}/{id:guid}", true] = OnGetContentAsync;

            Put["/{collection}/{id:guid}", true] = OnUpdateContentAsync;

            Delete["/{collection}/{id:guid}", true] = OnDeleteContentAsync;

            Patch["/{collection}/{id:guid}", true] = OnPatchContentAsync;
        }

        private async Task<object> OnPatchContentAsync(dynamic req, CancellationToken token)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var collectionName = (string)req["collection"];
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("collection cannot be null or blank");

            if (!_db.ContainsCollection(collectionName))
                return HttpStatusCode.NotFound;

            var guid = (Guid)req["id"];
            if (guid == Guid.Empty)
                throw new ArgumentException("id cannot be Guid.Empty");

            var excludedFields = new[] { "collection", "id" };
            var dictionary = this.Bind<DynamicDictionary>(excludedFields).ToDictionary();
            if (dictionary == null || dictionary.Count == 0)
                throw new InvalidOperationException("There is no data for this operation");

            var contentPatch = new Content(dictionary);
            var collection = _db[collectionName];

            var count = collection.Count(new SearchCriteria { Query = "_id:" + guid.ToString() });
            if (count == 0)
                return HttpStatusCode.NotFound;

            var content = await collection.GetAsync(guid);
            if (content == null)
                return HttpStatusCode.NotFound;

            var valuesToPatch = contentPatch.AsDictionary();
            var contentDictionary = content.AsDictionary();

            var keysToUpdate = valuesToPatch.Keys.Except(new[] { "collection", Content.ID_FIELD_NAME, Content.CREATED_TIMESTAMP_FIELD_NAME, Content.MODIFIED_TIMESTAMP_FIELD_NAME });
            foreach (var key in keysToUpdate)            
                contentDictionary[key] = valuesToPatch[key];

            var affected = await collection.UpdateAsync(content);

            stopwatch.Stop();

            var responseDto = new UpdateResponseDto
            {
                Elapsed = stopwatch.Elapsed.ToString(),
                AffectedCount = affected
            };

            return responseDto;
        }

        private object OnGetCount(dynamic req)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var collectionName = (string)req["collection"];
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("collection cannot be null or blank");

            if (!_db.ContainsCollection(collectionName))
                return HttpStatusCode.NotFound;            

            var collection = _db[collectionName];
            var excludedFields = new[] { "SortByField", "TopN", "ItemsPerPage", "PageNumber" };
            var searchCriteria = this.Bind<SearchCriteria>();
            searchCriteria.TopN = Int32.MaxValue;
            var count = collection.Count(searchCriteria);

            stopwatch.Stop();

            var responseDto = new CountResponseDto
            {
                Elapsed = stopwatch.Elapsed.ToString(),
                Count = count
            };

            return responseDto;
        }

        private async Task<object> OnDeleteContentAsync(dynamic req, CancellationToken token)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var collectionName = (string)req["collection"];
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("collection cannot be null or blank");

            if (!_db.ContainsCollection(collectionName))
                return HttpStatusCode.NotFound;

            var guid = (Guid)req["id"];
            if (guid == Guid.Empty)
                throw new ArgumentException("id cannot be Guid.Empty");

            var collection = _db[collectionName];

            var count = collection.Count(new SearchCriteria { Query = "_id:" + guid.ToString() });
            if (count == 0)
                return HttpStatusCode.NotFound;

            var affected = await collection.DeleteAsync(guid);

            stopwatch.Stop();

            var responseDto = new UpdateResponseDto
            {
                Elapsed = stopwatch.Elapsed.ToString(),
                AffectedCount = affected
            };

            return responseDto;
        }

        private async Task<object> OnUpdateContentAsync(dynamic req, CancellationToken token)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var collectionName = (string)req["collection"];
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("collection cannot be null or blank");

            if (!_db.ContainsCollection(collectionName))
                return HttpStatusCode.NotFound;

            var guid = (Guid)req["id"];
            if (guid == Guid.Empty)
                throw new ArgumentException("id cannot be Guid.Empty");

            var excludedFields = new[] { "collection", "id" };
            var dictionary = this.Bind<DynamicDictionary>(excludedFields).ToDictionary();
            if (dictionary == null || dictionary.Count == 0)
                throw new InvalidOperationException("There is no data for this operation");

            var content = new Content(dictionary);  
            content._id = guid;

            var collection = _db[collectionName];

            var count = collection.Count(new SearchCriteria { Query = "_id:" + guid.ToString() });
            if (count == 0)
                return HttpStatusCode.NotFound;

            var affected = await collection.UpdateAsync(content);

            stopwatch.Stop();

            var responseDto = new UpdateResponseDto
            {
                Elapsed = stopwatch.Elapsed.ToString(),
                AffectedCount = affected
            };

            return responseDto;
        }

        private async Task<object> OnGetContentAsync(dynamic req, CancellationToken token)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var collectionName = (string)req["collection"];
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("collection cannot be null or blank");

            var guid = (Guid)req["id"];
            if (guid == Guid.Empty)
                throw new ArgumentException("id cannot be Guid.Empty");

            if (!_db.ContainsCollection(collectionName))
                return HttpStatusCode.NotFound;

            var collection = _db[collectionName];            
            var content = await collection.GetAsync(guid);            
            if (content == null)
                return HttpStatusCode.NotFound;

            stopwatch.Stop();

            var responseDto = new ContentResposeDto
            {
                Elapsed = stopwatch.Elapsed.ToString(),
                Content = content.AsExpando()
            };

            return responseDto;
        }

        private object OnGetCollectionSchema(dynamic req)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var collectionName = (string)req["collection"];
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("collection cannot be null or blank");

            if (!_db.ContainsCollection(collectionName))
                return HttpStatusCode.NotFound;

            var collection = _db[collectionName];
            var schema = collection.GetSchema();

            stopwatch.Stop();

            var responseDto = new SchemaResponseDto
            {
                Elapsed = stopwatch.Elapsed.ToString(),
                Schema = schema
            };

            return responseDto;
        }

        private async Task<object> OnSearchContentsAsync(dynamic req, CancellationToken token)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var collectionName = (string)req["collection"];
            if (!_db.ContainsCollection(collectionName))
                return HttpStatusCode.NotFound;
                        
            var collection = _db[collectionName];
            var searchCriteria = this.Bind<SearchCriteria>();
            var result = await collection.SearchAsync(searchCriteria);

            stopwatch.Stop();

            var responseDto = new SearchResponseDto(searchCriteria, result.HitCount, result.TotalHitCount, result.PageCount)
            {
                Elapsed = stopwatch.Elapsed.ToString(),
                Items = result.Items.Select(c => c.AsExpando())
            };           

            return responseDto;            
        }

        private async Task<object> OnInsertContentAsync(dynamic req, CancellationToken token)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var collectionName = (string)req["collection"];
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("collection cannot be null or blank");

            var excludedFields = new[] { "collection" };
            var dictionary = this.Bind<DynamicDictionary>(excludedFields).ToDictionary();
            var content = new Content(dictionary);            
            
            var collection = _db[collectionName];            
            // The collection will be auto-created if it doesn't exist

            var guid = await collection.InsertAsync(content);  

            stopwatch.Stop();     

            var responseDto = new InsertResponseDto
            {
                _id = guid,
                Elapsed = stopwatch.Elapsed.ToString()
            };

            return responseDto;
        }
    }    
}
