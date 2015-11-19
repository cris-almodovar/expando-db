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

        public static SearchResponseDto Populate(this SearchResponseDto dto, SearchResult<Content> searchResult, IList<string> selectedFields)
        {
            if (dto == null)
                throw new ArgumentNullException("dto");
            if (searchResult == null)
                throw new ArgumentNullException("searchResult");

            dto.Where = searchResult.Query;
            dto.SortBy = searchResult.SortByField;
            dto.TopN = searchResult.TopN;
            dto.HitCount = searchResult.HitCount;
            dto.TotalHitCount = searchResult.TotalHitCount;
            dto.PageCount = searchResult.PageCount;
            dto.PageNumber = searchResult.PageNumber;            
            dto.ItemsPerPage = searchResult.ItemsPerPage;
            dto.Contents = searchResult.Items.Select(c => c.Project(selectedFields).AsExpando());

            return dto;
        }

        public static Content Project(this Content content, IList<string> projectedFields)
        {
            if (content == null)
                throw new ArgumentNullException("content");
            if (projectedFields == null)
                throw new ArgumentNullException("projectedFields");

            if (projectedFields.Count == 0)
                return content;

            var contentDictionary = content.AsDictionary();

            // Remove fields that are not in the projectedFields
            contentDictionary.Keys.Except(new[] { Content.ID_FIELD_NAME })
                             .Where(fieldName => !projectedFields.Contains(fieldName))
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
