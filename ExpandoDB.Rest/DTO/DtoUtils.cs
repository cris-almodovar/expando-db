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
                ItemsPerPage = dto.itemsPerPage ?? SearchCriteria.DEFAULT_ITEMS_PER_PAGE,
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
        /// Populates the given <see cref="SearchResponseDto"/> with data from the given <see cref="SearchRequestDto"/> and <see cref="SearchResult{TResult}"/> objects.
        /// </summary>
        /// <param name="responseDto">The SearchResponseDto object.</param>
        /// <param name="searchRequestDto">The SearchRequestDto object.</param>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="searchResult">The SearchResult object.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        public static SearchResponseDto PopulateWith(this SearchResponseDto responseDto, SearchRequestDto searchRequestDto, string collectionName, SearchResult<Document> searchResult)
        {
            if (searchRequestDto == null)
                throw new ArgumentNullException(nameof(searchRequestDto));
            if (searchResult == null)
                throw new ArgumentNullException(nameof(searchResult));

            responseDto.select = searchRequestDto.select;
            responseDto.from = collectionName;
            responseDto.where = searchRequestDto.where;
            responseDto.orderBy = searchRequestDto.orderBy;
            responseDto.topN = searchResult.TopN;
            responseDto.itemCount = searchResult.ItemCount;
            responseDto.totalHits = searchResult.TotalHits;
            responseDto.pageCount = searchResult.PageCount;
            responseDto.pageNumber = searchResult.PageNumber;
            responseDto.itemsPerPage = searchResult.ItemsPerPage;
            responseDto.highlight = searchResult.IncludeHighlight;
            responseDto.selectCategories = searchResult.SelectCategories;
            responseDto.topNCategories = searchResult.TopNCategories;
            responseDto.categories = searchResult.Categories.Select(c => c.ToCategoryDto());

            var fieldsToSelect = searchRequestDto.select.ToList();
            if (fieldsToSelect.Count > 0 && searchResult.IncludeHighlight)
                fieldsToSelect.Add(LuceneHighlighter.HIGHLIGHT_FIELD_NAME);

            responseDto.items = searchResult.Items.Select(c => c.Select(fieldsToSelect).AsExpando());

            return responseDto;
        }

        public static ExpandoObject PopulateWith(this ExpandoObject responseDto, SearchRequestDto searchRequestDto, string collectionName, SearchResult<Document> searchResult, TimeSpan elapsed)
        {
            if (searchRequestDto == null)
                throw new ArgumentNullException(nameof(searchRequestDto));
            if (searchResult == null)
                throw new ArgumentNullException(nameof(searchResult));

            dynamic dynamicDto = responseDto;
            dynamicDto.select = searchRequestDto.select;
            dynamicDto.from = collectionName;
            dynamicDto.where = searchRequestDto.where;
            dynamicDto.orderBy = searchRequestDto.orderBy;
            dynamicDto.topN = searchResult.TopN;
            dynamicDto.itemCount = searchResult.ItemCount;
            dynamicDto.totalHits = searchResult.TotalHits;
            dynamicDto.pageCount = searchResult.PageCount;
            dynamicDto.pageNumber = searchResult.PageNumber;
            dynamicDto.itemsPerPage = searchResult.ItemsPerPage;
            dynamicDto.highlight = searchResult.IncludeHighlight;
            dynamicDto.selectCategories = searchResult.SelectCategories;
            dynamicDto.topNCategories = searchResult.TopNCategories;
            dynamicDto.categories = searchResult.Categories.Select(c => c.ToCategoryDto());

            var fieldsToSelect = searchRequestDto.select.ToList();
            if (fieldsToSelect.Count > 0 && searchResult.IncludeHighlight)
                fieldsToSelect.Add(LuceneHighlighter.HIGHLIGHT_FIELD_NAME);

            dynamicDto.items = searchResult.Items.Select(c => c.Select(fieldsToSelect).AsExpando());
            dynamicDto.timestamp = DateTime.UtcNow;
            dynamicDto.elapsed = elapsed.ToString();

            return dynamicDto;
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
        /// Converts the given Category object to a DTO.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <returns></returns>
        public static CategoryDto ToCategoryDto(this Category category)
        {
            var dto = new CategoryDto
            {
                name = category.Name,
                count = category.Count
            };

            if ((category.Values?.Count ?? 0) > 0)
            {
                dto.values = (from c in category.Values
                             select c.ToCategoryDto())
                             ?.ToList();
            }

            return dto;
        }
    }    
}
