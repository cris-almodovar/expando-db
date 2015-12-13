using ExpandoDB.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Rest.DTO
{
    public static class DtoExtensions
    {
        public static SearchCriteria ToSearchCriteria(this SearchRequestDto dto)
        {
            var searchCriteria = new SearchCriteria
            {
                Query = dto.where,
                SortByField = dto.orderBy,
                TopN = dto.topN,
                ItemsPerPage = dto.itemsPerPage,
                PageNumber = dto.pageNumber
            };

            return searchCriteria;
        }

        public static SearchCriteria ToSearchCriteria(this CountRequestDto dto)
        {
            var searchCriteria = new SearchCriteria
            {
                Query = dto.where,
                TopN = int.MaxValue
            };

            return searchCriteria;
        }

        public static SearchResponseDto PopulateWith(this SearchResponseDto responseDto, SearchRequestDto searchRequestDto, string collectionName, SearchResult<Content> searchResult)
        {
            if (searchRequestDto == null)
                throw new ArgumentNullException("searchRequestDto");
            if (searchResult == null)
                throw new ArgumentNullException("searchResult");

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

            var fieldsToSelect = searchRequestDto.select.ToList();
            responseDto.items = searchResult.Items.Select(c => c.Select(fieldsToSelect).AsExpando()).ToList();

            return responseDto;
        }

        public static Content Select(this Content content, IList<string> selectedFields)
        {
            if (content == null)
                throw new ArgumentNullException("content");
            if (selectedFields == null)
                throw new ArgumentNullException("selectedFields");

            if (selectedFields.Count == 0)
                return content;

            var contentDictionary = content.AsDictionary();

            // Remove fields that are not in the selectedFields
            contentDictionary.Keys.Except(new[] { Content.ID_FIELD_NAME })
                             .Where(fieldName => !selectedFields.Contains(fieldName))
                             .ToList()
                             .ForEach(fieldName => contentDictionary.Remove(fieldName));

            // Content should now only contain the fields in the projectedFields list, plus the _id field (which is always included)
            return content;
        }

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
    }    
}
