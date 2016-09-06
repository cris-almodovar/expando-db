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
                SortByField = dto.orderBy,
                TopN = dto.topN ?? SearchCriteria.DEFAULT_TOP_N,
                ItemsPerPage = dto.documentsPerPage ?? SearchCriteria.DEFAULT_ITEMS_PER_PAGE,
                PageNumber = dto.pageNumber ?? 1, 
                IncludeHighlight = dto.highlight ?? false,
                SelectCategories = dto.selectCategories,
                TopNCategories = dto.topNCategories ?? SearchCriteria.DEFAULT_TOP_N_CATEGORIES                 
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
                TopN = 1  // We're not interested in the docs, just the total hits.
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
        /// Converts the given comma-separated string to an IList
        /// </summary>
        /// <param name="csvString">The CSV string.</param>
        /// <returns></returns>
        public static IList<string> ToList(this string csvString)
        {
            var list = new List<string>();
            if (!String.IsNullOrWhiteSpace(csvString))
            {
                var fields = csvString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                      .Select(fieldName => fieldName.Trim());

                list.AddRange(fields);
            }
            return list;
        }       

        /// <summary>
        /// Converts the given Category object to ExpandoObject.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <returns></returns>
        public static ExpandoObject ToExpando(this Category category)
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
            dynamic dynamicDto = new ExpandoObject();

            dynamicDto.from = collectionName;
            dynamicDto._id = docId;
            dynamicDto.timestamp = DateTime.UtcNow;
            dynamicDto.elapsed = elapsed.ToString();

            return dynamicDto;
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
            dynamic dynamicDto = new ExpandoObject();

            dynamicDto.from = schema.Name;
            dynamicDto.schema = schema.ToDocument().AsExpando();
            dynamicDto.timestamp = DateTime.UtcNow;
            dynamicDto.elapsed = elapsed.ToString();

            return dynamicDto;
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
            dynamic dynamicDto = new ExpandoObject();

            dynamicDto.schemas = schemas.Select(s => s.ToDocument().AsExpando());
            dynamicDto.timestamp = DateTime.UtcNow;
            dynamicDto.elapsed = elapsed.ToString();

            return dynamicDto;
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

            dynamic dynamicDto = new ExpandoObject();

            dynamicDto.select = searchRequestDto.select;
            dynamicDto.topN = searchResult.TopN;
            dynamicDto.from = collectionName;
            dynamicDto.where = searchRequestDto.where;
            dynamicDto.orderBy = searchRequestDto.orderBy;

            dynamicDto.documentCount = searchResult.ItemCount;
            dynamicDto.totalHits = searchResult.TotalHits;
            dynamicDto.pageCount = searchResult.PageCount;
            dynamicDto.pageNumber = searchResult.PageNumber;
            dynamicDto.documentsPerPage = searchResult.ItemsPerPage;
            dynamicDto.highlight = searchResult.IncludeHighlight;
            dynamicDto.selectCategories = searchResult.SelectCategories;
            dynamicDto.topNCategories = searchResult.TopNCategories;

            var fieldsToSelect = searchRequestDto.select.ToList();
            if (fieldsToSelect.Count > 0 && searchResult.IncludeHighlight)
                fieldsToSelect.Add(LuceneHighlighter.HIGHLIGHT_FIELD_NAME);

            dynamicDto.documents = searchResult.Items?.Select(c => c.Select(fieldsToSelect).AsExpando());
            dynamicDto.categories = searchResult.Categories?.Select(c => c.ToExpando());

            dynamicDto.timestamp = DateTime.UtcNow;
            dynamicDto.elapsed = elapsed.ToString();

            return dynamicDto;
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
            dynamic dynamicDto = new ExpandoObject();

            dynamicDto.from = collectionName;
            dynamicDto.where = where;
            dynamicDto.count = count;
            dynamicDto.timestamp = DateTime.UtcNow;
            dynamicDto.elapsed = elapsed.ToString();

            return dynamicDto;
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
            dynamic dynamicDto = new ExpandoObject();

            dynamicDto.from = collectionName;
            dynamicDto.document = document.AsExpando();
            dynamicDto.timestamp = DateTime.UtcNow;
            dynamicDto.elapsed = elapsed.ToString();

            return dynamicDto;
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
            dynamic dynamicDto = new ExpandoObject();

            dynamicDto.from = collectionName;
            dynamicDto.affectedCount = affectedCount;
            dynamicDto.timestamp = DateTime.UtcNow;
            dynamicDto.elapsed = elapsed.ToString();

            return dynamicDto;
        }

        /// <summary>
        /// Builds a response DTO for the DELETE (Drop) Collection request.
        /// </summary>
        /// <param name="dbService">The database service.</param>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="isDropped">if set to <c>true</c> the Collection was dropped.</param>
        /// <param name="elapsed">The elapsed time.</param>
        /// <returns></returns>
        public static ExpandoObject BuildDropResposeDto(this DbService dbService, string collectionName, bool isDropped, TimeSpan elapsed)
        {
            dynamic dynamicDto = new ExpandoObject();

            dynamicDto.from = collectionName;
            dynamicDto.isDropped = isDropped;
            dynamicDto.timestamp = DateTime.UtcNow;
            dynamicDto.elapsed = elapsed.ToString();

            return dynamicDto;
        }
    }    
}
