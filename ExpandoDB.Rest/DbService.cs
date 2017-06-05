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
using System.Collections.Generic;
using ExpandoDB.Serialization;
using Nancy.Metrics;
using Metrics;
using System.Dynamic;
using System.Collections;

namespace ExpandoDB.Rest
{
    /// <summary>
    /// Implements ExpandoDB's REST API; an instance of this class in created for each web request
    /// received by ExpandoDB.
    /// </summary>
    /// <seealso cref="Nancy.NancyModule" />
    public class DbService : NancyModule
    {
        private readonly Database _database;
        private readonly ILog _log = LogManager.GetLogger(typeof(DbService).Name);        

        /// <summary>
        /// Initializes a new instance of the <see cref="DbService"/> class.
        /// </summary>
        /// <param name="database">The Database instance; this is auto-injected by NancyFX's IOC container.</param>
        public DbService(Database database) : base("/db")
        {
            _database = database;            

            // Here we define the routes and their corresponding handlers.
            // Note that all handlers except OnGetCount() and OnGetCollectionSchema() are async.

            Get["/_schemas"] = OnGetDatabaseSchema;
            Get["/{collection}/_schema"] = OnGetCollectionSchema;

            Put["/{collection}/_schema"] = OnReplaceCollectionSchema;
            Post["/{collection}/_schema/fields"] = OnInsertSchemaField;
            Get["/{collection}/_schema/fields/{fieldName}"] = OnGetSchemaField;
            Put["/{collection}/_schema/fields/{fieldName}"] = OnReplaceSchemaField;
            Put["/{collection}/_schema/fields/{fieldName}/facetSettings"] = OnReplaceSchemaFieldFacetSettings;            

            Post["/{collection}", true] = OnInsertDocumentAsync;            
            Get["/{collection}", true] = OnSearchDocumentsAsync;

            Get["/{collection}/count"] = OnGetCount;

            Get["/{collection}/{id:guid}", true] = OnGetDocumentAsync;
            Put["/{collection}/{id:guid}", true] = OnReplaceDocumentAsync;                       
            Patch["/{collection}/{id:guid}", true] = OnPatchDocumentAsync;
            Delete["/{collection}/{id:guid}", true] = OnDeleteDocumentAsync;

            Delete["/{collection}", true] = OnDeleteCollectionAsync;
        }


        /// <summary>
        /// Inserts a new Document object into a Document Collection.
        /// </summary>
        /// <param name="req">The request object.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">collection cannot be null or blank</exception>
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

            // Get the target Document Collection; it will be auto-created if it doesn't exist.
            var collection = _database[collectionName];

            // Insert the Document object into the target Document Collection.
            var docId = await collection.InsertAsync(document).ConfigureAwait(false);

            stopwatch.Stop();

            var responseDto = this.BuildInsertResponseDto(collectionName, docId, stopwatch.Elapsed);
            return Response.AsJson(responseDto);
        }  
        
        /// <summary>
        /// Searches a Document Collection for Documents that match a query expression.
        /// </summary>
        /// <param name="req">The request object.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        private async Task<object> OnSearchDocumentsAsync(dynamic req, CancellationToken token)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var collectionName = (string)req["collection"];
            if (!_database.ContainsCollection(collectionName))
                return HttpStatusCode.NotFound;

            var collection = _database[collectionName];
            var requestDto = this.Bind<SearchRequestDto>();
            var searchCriteria = requestDto.ToSearchCriteria();            
            var result = await collection.SearchAsync(searchCriteria);

            stopwatch.Stop();            

            var responseDto = this.BuildSearchResponseDto(requestDto, collectionName, result, stopwatch.Elapsed);           
            return Response.AsJson(responseDto);
        }

        /// <summary>
        /// Returns the number of Document objects in a Document Collection that match a query expression.
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

            if (!_database.ContainsCollection(collectionName))
                return HttpStatusCode.NotFound;

            var collection = _database[collectionName];
            var countRequestDto = this.Bind<CountRequestDto>();
            var searchCriteria = countRequestDto.ToSearchCriteria();
            var count = collection.Count(searchCriteria);

            stopwatch.Stop();

            var responseDto = this.BuildCountResponseDto(collectionName, countRequestDto.where, count, stopwatch.Elapsed);
            return Response.AsJson(responseDto);
        }

        /// <summary>
        /// Gets a Document object identified by its id, from a Document Collection.
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

            if (!_database.ContainsCollection(collectionName))
                return HttpStatusCode.NotFound;

            var collection = _database[collectionName];
            var document = await collection.GetAsync(guid).ConfigureAwait(false);
            if (document == null)
                return HttpStatusCode.NotFound;

            // Check if there is a 'select' parameter; if yes then 
            // we only return the fields specified in the 'select' parameter.

            var requestDto = this.Bind<DocumentRequestDto>();
            var fieldsToSelect = requestDto.select.ToList();
            if (fieldsToSelect.Count > 0)
                document = document.Select(fieldsToSelect);

            stopwatch.Stop();

            var responseDto = this.BuildDocumentResponseDto(collectionName, document, stopwatch.Elapsed);
            return Response.AsJson(responseDto);
        }

        /// <summary>
        /// Replaces a Document object identified by its _id, in a Document Collection.
        /// </summary>
        /// <param name="req">The request object.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>       
        /// <exception cref="System.InvalidOperationException">There is no data for this operation</exception>
        private async Task<object> OnReplaceDocumentAsync(dynamic req, CancellationToken token)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var collectionName = (string)req["collection"];
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("collection cannot be null or blank");

            if (!_database.ContainsCollection(collectionName))
                return HttpStatusCode.NotFound;

            var guid = (Guid)req["id"];
            if (guid == Guid.Empty)
                throw new ArgumentException("id cannot be Guid.Empty");

            var collection = _database[collectionName];
            var existingDoc = await collection.GetAsync(guid);
            if (existingDoc == null)
                return HttpStatusCode.NotFound;

            var excludedFields = new[] { "collection", "id" };
            var dictionary = this.Bind<DynamicDictionary>(excludedFields).ToDictionary();
            if (dictionary == null || dictionary.Count == 0)
                throw new InvalidOperationException("There is no data for this operation");

            var document = new Document(dictionary);
            document._id = guid;

            if (document._modifiedTimestamp != null && document._modifiedTimestamp != existingDoc._modifiedTimestamp)            
                throw new InvalidOperationException("The document was modified by another user or process.");            

            var updatedCount = await collection.UpdateAsync(document).ConfigureAwait(false);

            stopwatch.Stop();

            var responseDto = this.BuildUpdateResponseDto(collectionName, updatedCount, stopwatch.Elapsed);
            return Response.AsJson(responseDto);
        }

        /// <summary>
        /// Updates specific fields of a Document object identified by its _id, in a Document Collection.
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

            if (!_database.ContainsCollection(collectionName))
                return HttpStatusCode.NotFound;

            var guid = (Guid)req["id"];
            if (guid == Guid.Empty)
                throw new ArgumentException("id cannot be Guid.Empty");

            // Read in the PATCH payload.            
            var patchOperations = this.Bind<IList<PatchOperationDto>>();
            if (patchOperations == null || patchOperations.Count == 0)
                throw new InvalidOperationException("PATCH operations must be specified.");
            
            // Retrieve the Document to be PATCHed.
            var collection = _database[collectionName];

            var count = collection.Count(new SearchCriteria { Query = $"{Schema.MetadataField.ID}: {guid}" });
            if (count == 0)
                return HttpStatusCode.NotFound;

            var document = await collection.GetAsync(guid).ConfigureAwait(false);
            if (document == null)
                return HttpStatusCode.NotFound;

            // Apply the PATCH operations to the Document
            foreach (var operation in patchOperations)
                operation.Apply(document);                     
           
            // Update the PATCHed Document.
            var updatedCount = await collection.UpdateAsync(document).ConfigureAwait(false);

            stopwatch.Stop();

            var responseDto = this.BuildUpdateResponseDto(collectionName, updatedCount, stopwatch.Elapsed);
            return Response.AsJson(responseDto);
        }

        /// <summary>
        /// Deletes a Document object identified by its _id, in a Document Collection.
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

            if (!_database.ContainsCollection(collectionName))
                return HttpStatusCode.NotFound;

            var guid = (Guid)req["id"];
            if (guid == Guid.Empty)
                throw new ArgumentException("id cannot be Guid.Empty");

            var collection = _database[collectionName];

            var count = collection.Count(new SearchCriteria { Query = $"{Schema.MetadataField.ID}: {guid}" });
            if (count == 0)
                return HttpStatusCode.NotFound;

            var deletedCount = await collection.DeleteAsync(guid).ConfigureAwait(false);

            stopwatch.Stop();

            var responseDto = this.BuildUpdateResponseDto(collectionName, deletedCount, stopwatch.Elapsed);
            return Response.AsJson(responseDto);
        }

        /// <summary>
        /// Deletes all Documents from the Collection, optionally dropping both data and index.
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

            if (collectionName == Schema.COLLECTION_NAME)
                throw new InvalidOperationException($"Cannot drop the {Schema.COLLECTION_NAME} collection.");

            if (!_database.ContainsCollection(collectionName))
                return HttpStatusCode.NotFound;

            var collection = _database[collectionName];
            var requestDto = this.Bind<DeleteCollectionDto>();
            var drop = requestDto.drop ?? false;
            var isDropped = false;

            if (drop)
                isDropped = await _database.DropCollectionAsync(collectionName).ConfigureAwait(false);
            else
                await collection.TruncateAsync().ConfigureAwait(false);     

            stopwatch.Stop();

            var responseDto = this.BuildDeleteCollectionResposeDto(collectionName, isDropped, stopwatch.Elapsed);
            return Response.AsJson(responseDto);
        }


        /// <summary>
        /// Creates a Schema for a Document Collection; if the Collection does not exist yet, it is auto-created.
        /// This operation is only valid for Collections that do not yet have a Schema, or Collections that only have
        /// the default empty Schema.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <returns></returns>        
        private dynamic OnReplaceCollectionSchema(dynamic req)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var collectionName = (string)req["collection"];
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("collection cannot be null or blank");

            if (collectionName.StartsWith("_", StringComparison.InvariantCulture))
                throw new InvalidOperationException($"Cannot change the Schema of system Collection '{collectionName}'.");

            var collection = _database[collectionName];
            var schema = collection.Schema;

            if (!schema.IsDefault())
                throw new InvalidOperationException($"The Schema of Collection '{collectionName}' is not empty, and hence cannot  be changed.");

            var excludedFields = new[] { "collection" };
            var dictionary = this.Bind<DynamicDictionary>(excludedFields).ToDictionary();
            var updatedSchema = dictionary.ToSchema();

            // We can only populate the Fields collection. 
            foreach (var field in updatedSchema.Fields)
                schema.Fields.TryAdd(field.Name, field);

            stopwatch.Stop();

            var responseDto = this.BuildUpdateResponseDto(collectionName, 1, stopwatch.Elapsed);
            return Response.AsJson(responseDto);
        }


        /// <summary>
        /// Returns the schema of a Document Collection; a schema is simply a set of fields and their corresponding data types.
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

            if (collectionName.StartsWith("_", StringComparison.InvariantCulture))
                throw new InvalidOperationException($"Cannot get the Schema of system Collection '{collectionName}'.");

            if (!_database.ContainsCollection(collectionName))
                return HttpStatusCode.NotFound;

            var collection = _database[collectionName];
            var schema = collection.Schema;

            stopwatch.Stop();

            var responseDto = this.BuildSchemaResponseDto(schema, stopwatch.Elapsed);
            return Response.AsJson(responseDto);
        }
                

        /// <summary>
        /// Returns the schemas of all Document Collections in the Database; 
        /// a Schema is simply a list of Fields and their corresponding Data Types.
        /// </summary>
        /// <param name="req">The request object.</param>
        /// <returns></returns>        
        private object OnGetDatabaseSchema(dynamic req)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var schemas = from collectionName in _database.GetCollectionNames().Except(new[] { Schema.COLLECTION_NAME }).OrderBy(n => n)
                          let schema = _database[collectionName]?.Schema
                          where schema != null
                          select schema;

            stopwatch.Stop();

            var responseDto = this.BuildSchemaResponseDto(schemas, stopwatch.Elapsed);
            return Response.AsJson(responseDto);
        }


        private dynamic OnInsertSchemaField(dynamic req)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var collectionName = (string)req["collection"];
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("collection cannot be null or blank");            

            if (collectionName.StartsWith("_", StringComparison.InvariantCulture))
                throw new InvalidOperationException($"Cannot change the Schema of system Collection '{collectionName}'.");

            if (!_database.ContainsCollection(collectionName))
                return HttpStatusCode.NotFound;

            var collection = _database[collectionName];
            var schema = collection.Schema;

            var excludedFields = new[] { "collection" };
            var dictionary = this.Bind<DynamicDictionary>(excludedFields).ToDictionary();
            var schemaField = new Schema.Field().PopulateWith(dictionary);

            if (schema.Fields.ContainsKey(schemaField.Name))
                throw new InvalidOperationException($"The field '{schemaField.Name}' already exists in Schema '{schema.Name}'");

            schema.Fields.TryAdd(schemaField.Name, schemaField);

            stopwatch.Stop();

            var responseDto = this.BuildUpdateResponseDto(collectionName, 1, stopwatch.Elapsed);
            return Response.AsJson(responseDto);
        }

        private dynamic OnGetSchemaField(dynamic req)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var collectionName = (string)req["collection"];
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("collection cannot be null or blank");

            var fieldName = (string)req["fieldName"];
            if (String.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("fieldName cannot be null or blank");

            if (collectionName == Schema.COLLECTION_NAME)
                throw new InvalidOperationException($"Cannot get the Schema of the '{Schema.COLLECTION_NAME}' collection.");

            if (!_database.ContainsCollection(collectionName))
                return HttpStatusCode.NotFound;

            var collection = _database[collectionName];
            var schema = collection.Schema;

            Schema.Field schemaField = null;
            schema.Fields.TryGetValue(fieldName, out schemaField);

            if (schemaField == null)
                return HttpStatusCode.NotFound;

            stopwatch.Stop();

            var responseDto = this.BuildSchemaFieldResponseDto(schemaField, collectionName, stopwatch.Elapsed);
            return Response.AsJson(responseDto);
        }

        /// <summary>
        /// Replaces the properties of the named Field.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        private dynamic OnReplaceSchemaField(dynamic req)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var collectionName = (string)req["collection"];
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("collection cannot be null or blank");

            var fieldName = (string)req["fieldName"];
            if (String.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("fieldName cannot be null or blank");

            if (collectionName.StartsWith("_", StringComparison.InvariantCulture))
                throw new InvalidOperationException($"Cannot change the Schema of system Collection '{collectionName}'.");

            if (!_database.ContainsCollection(collectionName))
                return HttpStatusCode.NotFound;

            var collection = _database[collectionName];
            var schema = collection.Schema;

            Schema.Field schemaField = null;
            schema.Fields.TryGetValue(fieldName, out schemaField);

            if (schemaField == null)
                return HttpStatusCode.NotFound;

            var excludedFields = new[] { "collection" };
            var dictionary = this.Bind<DynamicDictionary>(excludedFields).ToDictionary();
            var newSchemaField = new Schema.Field().PopulateWith(dictionary);

            // We can only update the Data Type if it is not yet set.
            if (newSchemaField.DataType != schemaField.DataType && schemaField.DataType != Schema.DataType.Null)
                throw new SchemaException($"Cannot change the Data Type of '{schemaField.Name}' from {schemaField.DataType} to {newSchemaField.DataType}");

            // We can update the IsTokenized property.
            if (dictionary.ContainsKey("IsTokenized"))
            {
                if (schemaField.DataType == Schema.DataType.Text ||
                    (schemaField.DataType == Schema.DataType.Array && schemaField.ArrayElementDataType == Schema.DataType.Text))
                    schemaField.IsTokenized = newSchemaField.IsTokenized;
            }

            // We can replace the FacetSettings if it is not yet set.
            if (dictionary.ContainsKey("FacetSettings"))
            {
                if (schemaField.FacetSettings != null)
                    throw new InvalidOperationException($"The field '{fieldName}' already has a FacetSettings object.");

                schemaField.FacetSettings = newSchemaField.FacetSettings;
            }

            stopwatch.Stop();

            var responseDto = this.BuildUpdateResponseDto(collectionName, 1, stopwatch.Elapsed);
            return Response.AsJson(responseDto);

        }

        private dynamic OnReplaceSchemaFieldFacetSettings(dynamic req)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var collectionName = (string)req["collection"];
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("collection cannot be null or blank");

            var fieldName = (string)req["fieldName"];
            if (String.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("fieldName cannot be null or blank");

            if (collectionName.StartsWith("_", StringComparison.InvariantCulture))
                throw new InvalidOperationException($"Cannot change the Schema of system Collection '{collectionName}'.");

            if (!_database.ContainsCollection(collectionName))
                return HttpStatusCode.NotFound;

            var collection = _database[collectionName];
            var schema = collection.Schema;

            Schema.Field schemaField = null;
            schema.Fields.TryGetValue(fieldName, out schemaField);

            if (schemaField == null)
                return HttpStatusCode.NotFound;            

            if (schemaField.FacetSettings != null)
                throw new InvalidOperationException($"The field '{fieldName}' already has a FacetSettings object.");

            var excludedFields = new[] { "collection" };
            var dictionary = this.Bind<DynamicDictionary>(excludedFields).ToDictionary();
            var facetSettings = new Schema.FacetSettings().PopulateWith(dictionary);

            schemaField.FacetSettings = facetSettings;

            stopwatch.Stop();

            var responseDto = this.BuildUpdateResponseDto(collectionName, 1, stopwatch.Elapsed);
            return Response.AsJson(responseDto);
        }
    }    
}
