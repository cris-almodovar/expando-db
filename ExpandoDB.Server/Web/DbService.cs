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

            var patchDictionary = contentPatch.AsDictionary();
            var contentDictionary = content.AsDictionary();

            var keysToUpdate = patchDictionary.Keys.Except(new[] { "collection", Content.ID_FIELD_NAME, Content.CREATED_TIMESTAMP_FIELD_NAME, Content.MODIFIED_TIMESTAMP_FIELD_NAME });
            foreach (var key in keysToUpdate)            
                contentDictionary[key] = patchDictionary[key];

            var affected = await collection.UpdateAsync(content);

            stopwatch.Stop();

            var responseDto = new UpdateResponseDto
            {
                elapsed = stopwatch.Elapsed.ToString(),
                fromCollection = collectionName,
                affectedCount = affected
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
            var countRequestDto = this.Bind<CountRequestDto>();
            var searchCriteria = countRequestDto.ToSearchCriteria();            
            var count = collection.Count(searchCriteria);

            stopwatch.Stop();

            var responseDto = new CountResponseDto
            {
                elapsed = stopwatch.Elapsed.ToString(),
                fromCollection = collectionName,
                where = countRequestDto.where,
                count = count
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
                elapsed = stopwatch.Elapsed.ToString(),
                fromCollection = collectionName,
                affectedCount = affected
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

            var collection = _db[collectionName];
            var count = collection.Count(new SearchCriteria { Query = "_id:" + guid.ToString() });
            if (count == 0)
                return HttpStatusCode.NotFound;

            var excludedFields = new[] { "collection", "id" };
            var dictionary = this.Bind<DynamicDictionary>(excludedFields).ToDictionary();
            if (dictionary == null || dictionary.Count == 0)
                throw new InvalidOperationException("There is no data for this operation");

            var content = new Content(dictionary);  
            content._id = guid;        
            var affected = await collection.UpdateAsync(content);

            stopwatch.Stop();

            var responseDto = new UpdateResponseDto
            {
                elapsed = stopwatch.Elapsed.ToString(),
                fromCollection = collectionName,
                affectedCount = affected
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
                elapsed = stopwatch.Elapsed.ToString(),
                fromCollection = collectionName,
                content = content.AsExpando()
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
                elapsed = stopwatch.Elapsed.ToString(),
                schema = schema
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
            var requestDto = this.Bind<SearchRequestDto>();
            var searchCriteria = requestDto.ToSearchCriteria();
            var selectedFields = requestDto.select.ToList();
            var result = await collection.SearchAsync(searchCriteria);

            stopwatch.Stop();

            var responseDto = new SearchResponseDto().Populate(requestDto, collectionName, result);
            responseDto.elapsed = stopwatch.Elapsed.ToString();           

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
                fromCollection = collectionName,
                elapsed = stopwatch.Elapsed.ToString()
            };

            return responseDto;
        }
    }    
}
