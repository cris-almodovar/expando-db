using ExpandoDB.Rest.DTO;
using ExpandoDB.Search;
using Common.Logging;
using Nancy;
using Nancy.ModelBinding;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ExpandoDB.Rest
{
    /// <summary>
    /// Implements ExpandoDB's REST API; an instance of this class in created for each web request
    /// received by ExpandoDB.
    /// </summary>
    /// <seealso cref="Nancy.NancyModule" />
    public class DbService : NancyModule
    {
        private readonly Database _db;
        private readonly ILog _log = LogManager.GetLogger(typeof(DbService).Name);

        /// <summary>
        /// Initializes a new instance of the <see cref="DbService"/> class.
        /// </summary>
        /// <param name="db">The Database instance; this is auto-injected by NancyFX's IOC container.</param>
        public DbService(Database db) : base("/db")
        {
            _db = db;

            // Here we define the routes and their corresponding handlers.
            // Note that all handlers except OnGetCount() and OnGetCollectionSchema() are async.

            Post["/{collection}", true] = OnInsertContentAsync;
            Get["/{collection}/schema"] = OnGetCollectionSchema;
            Get["/{collection}", true] = OnSearchContentsAsync;
            Get["/{collection}/count"] = OnGetCount;
            Get["/{collection}/{id:guid}", true] = OnGetContentAsync;
            Put["/{collection}/{id:guid}", true] = OnUpdateContentAsync;
            Patch["/{collection}/{id:guid}", true] = OnPatchContentAsync;
            Delete["/{collection}/{id:guid}", true] = OnDeleteContentAsync;
            Delete["/{collection}", true] = OnDeleteCollectionAsync;
        }

        /// <summary>
        /// Inserts a new Content object into a ContentCollection.
        /// </summary>
        /// <param name="req">The request object.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>        
        private async Task<object> OnInsertContentAsync(dynamic req, CancellationToken token)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var collectionName = (string)req["collection"];
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("collection cannot be null or blank");

            // Get the Content object from the request.
            // Note: Bind() will get resolved to DynamicModelBinder.Bind().
            var excludedFields = new[] { "collection" };
            var dictionary = this.Bind<DynamicDictionary>(excludedFields).ToDictionary();
            var content = new Content(dictionary);

            // Get the target ContentCollection; it will be auto-created if it doesn't exist.
            var collection = _db[collectionName];            

            // Insert the Content object into the target ContentCollection.
            var guid = await collection.InsertAsync(content).ConfigureAwait(false);

            stopwatch.Stop();

            var responseDto = new InsertResponseDto
            {
                timestamp = DateTime.UtcNow,
                elapsed = stopwatch.Elapsed.ToString(),
                from = collectionName,
                _id = guid
            };

            return responseDto;
        }

        /// <summary>
        /// Returns the schema of a ContentCollection; a schema is simply a set of fields and their corresponding data types.
        /// </summary>
        /// <param name="req">The request object.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">collection cannot be null or blank</exception>
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
                timestamp = DateTime.UtcNow,
                elapsed = stopwatch.Elapsed.ToString(),
                schema = schema
            };

            return responseDto;
        }

        /// <summary>
        /// Searches a ContentCollection for Contents that match a query expression.
        /// </summary>
        /// <param name="req">The request object.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
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
            var result = await collection.SearchAsync(searchCriteria).ConfigureAwait(false);

            stopwatch.Stop();

            var responseDto = new SearchResponseDto().PopulateWith(requestDto, collectionName, result);
            responseDto.timestamp = DateTime.UtcNow;
            responseDto.elapsed = stopwatch.Elapsed.ToString();

            return responseDto;
        }

        /// <summary>
        /// Returns the number of Content objects in a ContentCollection that match a query expression.
        /// </summary>
        /// <param name="req">The request object.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">collection cannot be null or blank</exception>
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
                timestamp = DateTime.UtcNow,
                elapsed = stopwatch.Elapsed.ToString(),
                from = collectionName,
                where = countRequestDto.where,
                count = count
            };

            return responseDto;
        }

        /// <summary>
        /// Gets a Content object identified by its id, from a ContentCollection.
        /// </summary>
        /// <param name="req">The request object.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>        
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
            var content = await collection.GetAsync(guid).ConfigureAwait(false);
            if (content == null)
                return HttpStatusCode.NotFound;

            stopwatch.Stop();

            var responseDto = new ContentResposeDto
            {
                timestamp = DateTime.UtcNow,
                elapsed = stopwatch.Elapsed.ToString(),
                from = collectionName,
                content = content.AsExpando()
            };

            return responseDto;
        }

        /// <summary>
        /// Replaces a Content object identified by its _id, in a ContentCollection.
        /// </summary>
        /// <param name="req">The request object.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>       
        /// <exception cref="System.InvalidOperationException">There is no data for this operation</exception>
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
            var count = collection.Count(new SearchCriteria { Query = $"{Content.ID_FIELD_NAME}: {guid}" });
            if (count == 0)
                return HttpStatusCode.NotFound;

            var excludedFields = new[] { "collection", "id" };
            var dictionary = this.Bind<DynamicDictionary>(excludedFields).ToDictionary();
            if (dictionary == null || dictionary.Count == 0)
                throw new InvalidOperationException("There is no data for this operation");

            var content = new Content(dictionary);
            content._id = guid;
            var affected = await collection.UpdateAsync(content).ConfigureAwait(false);

            stopwatch.Stop();

            var responseDto = new UpdateResponseDto
            {
                timestamp = DateTime.UtcNow,
                elapsed = stopwatch.Elapsed.ToString(),
                from = collectionName,
                affectedCount = affected
            };

            return responseDto;
        }

        /// <summary>
        /// Updates specific fields of a Content object identified by its _id, in a ContentCollection.
        /// </summary>
        /// <param name="req">The request object.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>       
        /// <exception cref="System.InvalidOperationException">There is no data for this operation</exception>
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

            var count = collection.Count(new SearchCriteria { Query = $"{Content.ID_FIELD_NAME}: {guid}" });
            if (count == 0)
                return HttpStatusCode.NotFound;

            var content = await collection.GetAsync(guid).ConfigureAwait(false);
            if (content == null)
                return HttpStatusCode.NotFound;

            var patchDictionary = contentPatch.AsDictionary();
            var contentDictionary = content.AsDictionary();

            var keysToUpdate = patchDictionary.Keys.Except(new[] { "collection", Content.ID_FIELD_NAME, Content.CREATED_TIMESTAMP_FIELD_NAME, Content.MODIFIED_TIMESTAMP_FIELD_NAME });
            foreach (var key in keysToUpdate)
                contentDictionary[key] = patchDictionary[key];

            var affected = await collection.UpdateAsync(content).ConfigureAwait(false);

            stopwatch.Stop();

            var responseDto = new UpdateResponseDto
            {
                timestamp = DateTime.UtcNow,
                elapsed = stopwatch.Elapsed.ToString(),
                from = collectionName,
                affectedCount = affected
            };

            return responseDto;
        }

        /// <summary>
        /// Deletes a Content object identified by its _id, in a ContentCollection.
        /// </summary>
        /// <param name="req">The request object.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>       
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

            var count = collection.Count(new SearchCriteria { Query = $"{Content.ID_FIELD_NAME}: {guid}" });
            if (count == 0)
                return HttpStatusCode.NotFound;

            var affected = await collection.DeleteAsync(guid).ConfigureAwait(false);

            stopwatch.Stop();

            var responseDto = new UpdateResponseDto
            {
                timestamp = DateTime.UtcNow,
                elapsed = stopwatch.Elapsed.ToString(),
                from = collectionName,
                affectedCount = affected
            };

            return responseDto;
        }

        /// <summary>
        /// Deletes the entire ContentCollection.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">collection cannot be null or blank</exception>
        private async Task<object> OnDeleteCollectionAsync(dynamic req, CancellationToken token)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var collectionName = (string)req["collection"];
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("collection cannot be null or blank");

            if (!_db.ContainsCollection(collectionName))
                return HttpStatusCode.NotFound;

            var isDropped = await _db.DropCollectionAsync(collectionName).ConfigureAwait(false);            

            stopwatch.Stop();

            var responseDto = new DropCollectionResponseDto
            {
                timestamp = DateTime.UtcNow,
                elapsed = stopwatch.Elapsed.ToString(),
                isDropped = isDropped
            };

            return responseDto;
        }
        
    }    
}
