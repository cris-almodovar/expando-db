﻿using ExpandoDB.Rest.DTO;
using ExpandoDB.Search;
using Common.Logging;
using Nancy;
using Nancy.ModelBinding;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using ExpandoDB.Serialization;

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

            Get["/_schemas"] = OnGetDatabaseSchema;
            Get["/_schemas/{collection}"] = OnGetCollectionSchema;
            Post["/{collection}", true] = OnInsertDocumentAsync;            
            Get["/{collection}", true] = OnSearchDocumentsAsync;            
            Get["/{collection}/count"] = OnGetCount;
            Get["/{collection}/{id:guid}", true] = OnGetDocumentAsync;
            Put["/{collection}/{id:guid}", true] = OnUpdateDocumentAsync;                       
            Patch["/{collection}/{id:guid}", true] = OnPatchDocumentAsync;
            Delete["/{collection}/{id:guid}", true] = OnDeleteDocumentAsync;
            Delete["/{collection}", true] = OnDeleteCollectionAsync;
        }

        

        /// <summary>
        /// Inserts a new Document object into a DocumentCollection.
        /// </summary>
        /// <param name="req">The request object.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>        
        private async Task<object> OnInsertDocumentAsync(dynamic req, CancellationToken token)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var collectionName = (string)req["collection"];
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("collection cannot be null or blank");

            // Get the Document object from the request.
            // Note: Bind() will get resolved to DynamicModelBinder.Bind().
            var excludedFields = new[] { "collection" };
            var dictionary = this.Bind<DynamicDictionary>(excludedFields).ToDictionary();
            var document = new Document(dictionary);

            // Get the target DocumentCollection; it will be auto-created if it doesn't exist.
            var collection = _db[collectionName];

            // Insert the Document object into the target DocumentCollection.
            var guid = await collection.InsertAsync(document).ConfigureAwait(false);

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
        /// Returns the schema of a DocumentCollection; a schema is simply a set of fields and their corresponding data types.
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
        /// Returns the schemas of all DocumentCollections in the database; a schema is simply a set of fields and their corresponding data types.
        /// </summary>
        /// <param name="req">The request object.</param>
        /// <returns></returns>        
        private object OnGetDatabaseSchema(dynamic req)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var schemas = (from collectionName in _db.GetCollectionNames().OrderBy(n=> n)
                          let schema = _db[collectionName]?.GetSchema()
                          where schema != null
                          select schema).ToList();                        

            stopwatch.Stop();

            var responseDto = new DatabaseSchemaResponseDto
            {
                timestamp = DateTime.UtcNow,
                elapsed = stopwatch.Elapsed.ToString(),
                schemas = schemas
            };

            return responseDto;
        }

        /// <summary>
        /// Searches a DocumentCollection for Documents that match a query expression.
        /// </summary>
        /// <param name="req">The request object.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        private async Task<object> OnSearchDocumentsAsync(dynamic req, CancellationToken token)
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

            var responseDto = new SearchResponseDto().PopulateWith(requestDto, collectionName, result);
            responseDto.timestamp = DateTime.UtcNow;
            responseDto.elapsed = stopwatch.Elapsed.ToString();

            return responseDto;
        }

        /// <summary>
        /// Returns the number of Document objects in a DocumentCollection that match a query expression.
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
        /// Gets a Document object identified by its id, from a DocumentCollection.
        /// </summary>
        /// <param name="req">The request object.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>        
        private async Task<object> OnGetDocumentAsync(dynamic req, CancellationToken token)
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
            var document = await collection.GetAsync(guid).ConfigureAwait(false);
            if (document == null)
                return HttpStatusCode.NotFound;

            stopwatch.Stop();

            var responseDto = new DocumentResposeDto
            {
                timestamp = DateTime.UtcNow,
                elapsed = stopwatch.Elapsed.ToString(),
                from = collectionName,
                document = document.AsExpando()
            };

            return responseDto;
        }

        /// <summary>
        /// Replaces a Document object identified by its _id, in a DocumentCollection.
        /// </summary>
        /// <param name="req">The request object.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>       
        /// <exception cref="System.InvalidOperationException">There is no data for this operation</exception>
        private async Task<object> OnUpdateDocumentAsync(dynamic req, CancellationToken token)
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
            var count = collection.Count(new SearchCriteria { Query = $"{Document.ID_FIELD_NAME}: {guid}" });
            if (count == 0)
                return HttpStatusCode.NotFound;

            var excludedFields = new[] { "collection", "id" };
            var dictionary = this.Bind<DynamicDictionary>(excludedFields).ToDictionary();
            if (dictionary == null || dictionary.Count == 0)
                throw new InvalidOperationException("There is no data for this operation");

            var document = new Document(dictionary);
            document._id = guid;
            var affected = await collection.UpdateAsync(document).ConfigureAwait(false);

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
        /// Updates specific fields of a Document object identified by its _id, in a DocumentCollection.
        /// </summary>
        /// <param name="req">The request object.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>       
        /// <exception cref="System.InvalidOperationException">There is no data for this operation</exception>
        private async Task<object> OnPatchDocumentAsync(dynamic req, CancellationToken token)
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

            // Read in the PATCH payload.            
            var patchOperations = this.Bind<IList<PatchOperationDto>>();
            if (patchOperations == null || patchOperations.Count == 0)
                throw new InvalidOperationException("PATCH operations must be specified.");
            
            // Retrieve the Document to be PATCHed.
            var collection = _db[collectionName];

            var count = collection.Count(new SearchCriteria { Query = $"{Document.ID_FIELD_NAME}: {guid}" });
            if (count == 0)
                return HttpStatusCode.NotFound;

            var document = await collection.GetAsync(guid).ConfigureAwait(false);
            if (document == null)
                return HttpStatusCode.NotFound;

            // Apply the PATCH operations to the Document
            foreach (var operation in patchOperations)
                operation.Apply(document);                     
           
            // Update the PATCHed Document.
            var affected = await collection.UpdateAsync(document).ConfigureAwait(false);

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
        /// Deletes a Document object identified by its _id, in a DocumentCollection.
        /// </summary>
        /// <param name="req">The request object.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>       
        private async Task<object> OnDeleteDocumentAsync(dynamic req, CancellationToken token)
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

            var count = collection.Count(new SearchCriteria { Query = $"{Document.ID_FIELD_NAME}: {guid}" });
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
        /// Deletes the entire DocumentCollection.
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
