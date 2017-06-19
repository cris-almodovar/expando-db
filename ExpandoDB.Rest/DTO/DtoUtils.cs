using ExpandoDB.Search;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace ExpandoDB.Rest.DTO
{
    /// <summary>
    /// Implements utitlity methods for ExpandoDB Data Transfer Objects (DTOs).
    /// </summary>
    public static class DtoUtils
    { 

        /// <summary>
        /// Converts the given <see cref="SearchRequestDto"/> to a <see cref="SearchCriteria"/> object./>
        /// </summary>
        /// <param name="dto">The SearchRequestDto object.</param>
        /// <returns></returns>
        public static SearchCriteria ToSearchCriteria(this SearchRequestDto dto)
        {
            var searchCriteria = new SearchCriteria
            {
                Query = dto.where,
                SortByFields = dto.orderBy,
                TopN = dto.topN,
                ItemsPerPage = dto.itemsPerPage,
                PageNumber = dto.pageNumber, 
                IncludeHighlight = dto.highlight,
                FacetFilters = dto.whereFacets,
                TopNFacets = dto.topNFacets               
            };

            return searchCriteria;
        }

        /// <summary>
        /// Converts the given <see cref="CountRequestDto"/> to a <see cref="SearchCriteria"/> object./>
        /// </summary>
        /// <param name="dto">The CountRequestDto object.</param>
        /// <returns></returns>
        public static SearchCriteria ToSearchCriteria(this CountRequestDto dto)
        {
            var searchCriteria = new SearchCriteria
            {
                Query = dto.where,
                TopN = 0,  // We're not interested in the docs, just the total hits.
                TopNFacets = 0
            };

            return searchCriteria;
        }        

        /// <summary>
        /// Selects only the specified list of fields.
        /// </summary>
        /// <param name="document">The Document object.</param>
        /// <param name="selectedFields">The list of fields to be selected.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        public static Document Select(this Document document, IList<string> selectedFields)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (selectedFields == null)
                throw new ArgumentNullException(nameof(selectedFields));

            if (selectedFields.Count == 0)
                return document;

            selectedFields = selectedFields.Distinct().ToList();

            var documentDictionary = document.AsDictionary();

            // Remove fields that are not in the selectedFields
            var keysToRemove = documentDictionary.Keys
                                                .Where(fieldName => !selectedFields.Contains(fieldName))
                                                .ToList();

            keysToRemove.ForEach(fieldName => documentDictionary.Remove(fieldName));

            // Document should now only contain the fields in the selectedFields list
            return document;
        }           

        /// <summary>
        /// Converts the given Category object to ExpandoObject.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <returns></returns>
        public static ExpandoObject ToExpando(this FacetValue category)
        {
            dynamic dto = new ExpandoObject();
            dto.name = category.Name;
            dto.count = category.Count;

            if ((category.Values?.Count ?? 0) > 0)
                dto.values = (from c in category.Values
                              select c.ToExpando())
                             ?.ToList();
            else
                dto.values = null;

            return dto;
        }

        /// <summary>
        /// Builds a response DTO for a POST (Insert) request.
        /// </summary>
        /// <param name="dbService">The database service.</param>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="docId">The identifier.</param>
        /// <param name="elapsed">The elapsed time.</param>
        /// <returns></returns>
        public static ExpandoObject BuildInsertResponseDto(this DbService dbService, string collectionName, Guid docId, TimeSpan elapsed)
        {
            dynamic responseDto = new ExpandoObject();

            responseDto.from = collectionName;
            responseDto._id = docId;
            responseDto.timestamp = DateTime.UtcNow;
            responseDto.elapsed = elapsed.ToString();

            return responseDto;
        }

        /// <summary>
        /// Builds a response DTO for the GET Schema request.
        /// </summary>
        /// <param name="dbService">The database service.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="elapsed">The elapsed time.</param>
        /// <returns></returns>
        public static ExpandoObject BuildSchemaResponseDto(this DbService dbService, Schema schema, TimeSpan elapsed)
        {
            dynamic responseDto = new ExpandoObject();

            responseDto.from = schema.Name;
            responseDto.schema = schema.ToDocument().AsExpando();
            responseDto.timestamp = DateTime.UtcNow;
            responseDto.elapsed = elapsed.ToString();

            return responseDto;
        }

        /// <summary>
        /// Builds a response DTO for the GET Schemas request.
        /// </summary>
        /// <param name="dbService">The database service.</param>
        /// <param name="schemas">The schemas.</param>
        /// <param name="elapsed">The elapsed time.</param>
        /// <returns></returns>
        public static ExpandoObject BuildSchemaResponseDto(this DbService dbService, IEnumerable<Schema> schemas, TimeSpan elapsed)
        {
            dynamic responseDto = new ExpandoObject();

            responseDto.schemas = schemas.Select(s => s.ToDocument().AsExpando());
            responseDto.timestamp = DateTime.UtcNow;
            responseDto.elapsed = elapsed.ToString();

            return responseDto;
        }

        /// <summary>
        /// Builds a response DTO for GET Documents (via Search) requests, using data from the given <see cref="SearchRequestDto" /> and <see cref="SearchResult{TResult}" /> objects.
        /// </summary>
        /// <param name="dbService">The database service.</param>
        /// <param name="searchRequestDto">The search request dto.</param>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="searchResult">The search result.</param>
        /// <param name="elapsed">The elapsed time.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public static ExpandoObject BuildSearchResponseDto(this DbService dbService, SearchRequestDto searchRequestDto, string collectionName, SearchResult<Document> searchResult, TimeSpan elapsed)
        {
            if (searchRequestDto == null)
                throw new ArgumentNullException(nameof(searchRequestDto));
            if (searchResult == null)
                throw new ArgumentNullException(nameof(searchResult));

            dynamic responseDto = new ExpandoObject();

            responseDto.select = searchRequestDto.select;
            responseDto.topN = searchResult.TopN ?? SearchCriteria.DEFAULT_TOP_N;
            responseDto.from = collectionName;
            responseDto.where = searchRequestDto.where;
            responseDto.orderBy = searchRequestDto.orderBy;

            responseDto.itemCount = searchResult.ItemCount;
            responseDto.totalHits = searchResult.TotalHits;
            responseDto.pageCount = searchResult.PageCount;
            responseDto.pageNumber = searchResult.PageNumber ?? 1;
            responseDto.itemsPerPage = searchResult.ItemsPerPage ?? SearchCriteria.DEFAULT_ITEMS_PER_PAGE;
            responseDto.highlight = searchResult.IncludeHighlight;
            responseDto.selectFacets = searchResult.FacetsToReturn;
            responseDto.whereFacets = searchResult.FacetFilters;
            responseDto.topNFacets = searchResult.TopNFacets ?? SearchCriteria.DEFAULT_TOP_N_FACETS;

            var fieldsToSelect = searchRequestDto.select.ToList();
            if (fieldsToSelect.Count > 0 && searchResult.IncludeHighlight == true)
                fieldsToSelect.Add(LuceneHighlighter.HIGHLIGHT_FIELD_NAME);

            responseDto.items = searchResult.Items?.Select(c => c.Select(fieldsToSelect).AsExpando());
            responseDto.facets = searchResult.Facets?.Select(c => c.ToExpando());

            responseDto.timestamp = DateTime.UtcNow;
            responseDto.elapsed = elapsed.ToString();

            return responseDto;
        }

        /// <summary>
        /// Builds a response DTO for the GET Collection Count request.
        /// </summary>
        /// <param name="dbService">The database service.</param>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="where">The where.</param>
        /// <param name="count">The count.</param>
        /// <param name="elapsed">The elapsed time.</param>
        /// <returns></returns>
        public static ExpandoObject BuildCountResponseDto(this DbService dbService, string collectionName, string where, int count, TimeSpan elapsed)
        {
            dynamic responseDto = new ExpandoObject();

            responseDto.from = collectionName;
            responseDto.where = where;
            responseDto.count = count;
            responseDto.timestamp = DateTime.UtcNow;
            responseDto.elapsed = elapsed.ToString();

            return responseDto;
        }

        /// <summary>
        /// Builds a response DTO for the GET Document (via ID lookup) request.
        /// </summary>
        /// <param name="dbService">The database service.</param>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="document">The document.</param>
        /// <param name="elapsed">The elapsed time.</param>
        /// <returns></returns>
        public static ExpandoObject BuildDocumentResponseDto(this DbService dbService, string collectionName, Document document, TimeSpan elapsed)
        {
            dynamic responseDto = new ExpandoObject();

            responseDto.from = collectionName;
            responseDto.document = document.AsExpando();
            responseDto.timestamp = DateTime.UtcNow;
            responseDto.elapsed = elapsed.ToString();

            return responseDto;
        }

        /// <summary>
        /// Builds a response DTO for the PUT, PATCH, DELETE requests.
        /// </summary>
        /// <param name="dbService">The database service.</param>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="affectedCount">The number of affected Documents.</param>
        /// <param name="elapsed">The elapsed time.</param>
        /// <returns></returns>
        public static ExpandoObject BuildUpdateResponseDto(this DbService dbService, string collectionName, int affectedCount, TimeSpan elapsed)
        {
            dynamic responseDto = new ExpandoObject();

            responseDto.from = collectionName;
            responseDto.affectedCount = affectedCount;
            responseDto.timestamp = DateTime.UtcNow;
            responseDto.elapsed = elapsed.ToString();

            return responseDto;
        }

        /// <summary>
        /// Builds a response DTO for the DELETE Collection request.
        /// </summary>
        /// <param name="dbService">The database service.</param>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="isDropped">if set to <c>true</c> the Collection was dropped.</param>
        /// <param name="elapsed">The elapsed time.</param>
        /// <returns></returns>
        public static ExpandoObject BuildDeleteCollectionResposeDto(this DbService dbService, string collectionName, bool isDropped, TimeSpan elapsed)
        {
            dynamic responseDto = new ExpandoObject();

            responseDto.from = collectionName;
            responseDto.isDropped = isDropped;
            responseDto.timestamp = DateTime.UtcNow;
            responseDto.elapsed = elapsed.ToString();

            return responseDto;
        }

        /// <summary>
        /// Builds a response DTO for the GET Schema Field request.
        /// </summary>
        /// <param name="dbService">The database service.</param>
        /// <param name="collectionName">The collection.</param>
        /// <param name="schemaField">The schema field.</param>
        /// <param name="elapsed">The elapsed time.</param>
        /// <returns></returns>
        public static ExpandoObject BuildSchemaFieldResponseDto(this DbService dbService, Schema.Field schemaField, string collectionName, TimeSpan elapsed)
        {
            dynamic responseDto = new ExpandoObject();

            responseDto.from = collectionName;
            responseDto.field = new Document(schemaField.ToDictionary()).AsExpando();
            responseDto.timestamp = DateTime.UtcNow;
            responseDto.elapsed = elapsed.ToString();

            return responseDto;
        }
    }    
}
