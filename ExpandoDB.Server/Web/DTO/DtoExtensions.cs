using ExpandoDB.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Server.Web.DTO
{
    public static class DtoExtensions
    {
        public static SearchCriteria ToSearchCriteria(this SearchRequestDto dto)
        {
            var searchCriteria = new SearchCriteria
            {
                Query = dto.Where,
                SortByField = dto.SortBy,
                TopN = dto.TopN,
                ItemsPerPage = dto.ItemsPerPage,
                PageNumber = dto.PageNumber
            };

            return searchCriteria;
        }

        public static SearchCriteria ToSearchCriteria(this CountRequestDto dto)
        {
            var searchCriteria = new SearchCriteria
            {
                Query = dto.Where,
                TopN = int.MaxValue
            };

            return searchCriteria;
        }

        public static SearchResponseDto Populate(this SearchResponseDto responseDto, SearchRequestDto searchRequestDto, string collectionName, SearchResult<Content> searchResult)
        {
            if (searchRequestDto == null)
                throw new ArgumentNullException("searchRequestDto");
            if (searchResult == null)
                throw new ArgumentNullException("searchResult");

            responseDto.Select = searchRequestDto.Select;
            responseDto.FromCollection = collectionName;
            responseDto.Where = searchRequestDto.Where;
            responseDto.SortBy = searchRequestDto.SortBy;
            responseDto.TopN = searchResult.TopN;
            responseDto.ItemCount = searchResult.ItemCount;
            responseDto.TotalHits = searchResult.TotalHits;
            responseDto.PageCount = searchResult.PageCount;
            responseDto.PageNumber = searchResult.PageNumber;
            responseDto.ItemsPerPage = searchResult.ItemsPerPage;

            var selectedFields = searchRequestDto.Select.ToList();
            responseDto.Contents = searchResult.Items.Select(c => c.Select(selectedFields).AsExpando()).ToList();

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
