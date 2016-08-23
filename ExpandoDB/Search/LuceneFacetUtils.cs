using FlexLucene.Facet;
using FlexLucene.Facet.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Search
{
    /// <summary>
    /// 
    /// </summary>
    public static class LuceneFacetUtils
    {
        /// <summary>
        /// Converts the specified category string to a Lucene FacetField.
        /// </summary>
        /// <param name="categoryString">The category.</param>
        /// <returns></returns>
        public static FacetField ToLuceneFacetField(this string categoryString)
        {
            const string COLON = ":";
            const string ESCAPED_COLON = @"\:";
            const string ESCAPED_COLON_TEMP_TOKEN = @"<<€€€€€€>>";
            const string SLASH = @"/";
            const string ESCAPED_SLASH = @"\/";
            const string ESCAPED_SLASH_TEMP_TOKEN = @"<<$$$$$$>>";
            
            // Sample categoryString:  "Author:Arthur Dent" => facet name is "Author", value is "Arthur Dent"

            if (String.IsNullOrWhiteSpace(categoryString))
                throw new ArgumentNullException(nameof(categoryString));

            // Convert \/ to <<$$$$$$>> and \: to <<€€€€€€>>
            var categoryStringCopy = categoryString.Trim()
                                                   .Replace(ESCAPED_SLASH, ESCAPED_SLASH_TEMP_TOKEN)
                                                   .Replace(ESCAPED_COLON, ESCAPED_COLON_TEMP_TOKEN);

            // Split the string using ":" to get the facet name
            var facetName = categoryStringCopy.Contains(COLON) ?
                            categoryStringCopy.Split(new[] { COLON }, StringSplitOptions.None).FirstOrDefault() :
                            null;

            if (String.IsNullOrWhiteSpace(facetName))
                throw new SchemaException($"Invalid category string: '{categoryString}'");

            facetName = facetName.Trim()
                                 .Replace(ESCAPED_SLASH_TEMP_TOKEN, SLASH)
                                 .Replace(ESCAPED_COLON, COLON);

            var facetValueStartIndex = categoryStringCopy.IndexOf(COLON, StringComparison.InvariantCulture) + 1;
            categoryStringCopy = categoryStringCopy.Substring(facetValueStartIndex);

            // Split the string using / as separateor to get the facet values
            var categoryParts = categoryStringCopy.Split(new[] { SLASH }, StringSplitOptions.None);
            if (categoryParts.Count() >= 1)
            {  
                var facetValues = categoryParts.Select(s => s.Trim()
                                                             .Replace(ESCAPED_SLASH_TEMP_TOKEN, SLASH)
                                                             .Replace(ESCAPED_COLON_TEMP_TOKEN, COLON))
                                               .ToArray();

                var facetField = new FacetField(facetName, facetValues);
                return facetField;
            }

            return null;
        }

        /// <summary>
        /// Determines whether the FacetsConfig contains a Facet with the specified facet name.
        /// </summary>
        /// <param name="facetsConfig">The facets configuration.</param>
        /// <param name="facetName">The name of the facet.</param>
        /// <returns>
        ///   <c>true</c> if the FacetsConfig contains a Facet with the specified facet name; otherwise, <c>false</c>.
        /// </returns>
        public static bool Contains(this FacetsConfig facetsConfig, string facetName)
        {
            if (facetsConfig == null)
                throw new ArgumentNullException(nameof(facetsConfig));
            if (facetName == null)
                throw new ArgumentNullException(nameof(facetName));

            var configs = facetsConfig.GetDimConfigs();
            if (configs != null)            
                return configs.containsKey(facetName);
            
            return false;
        }

        /// <summary>
        /// Checks if the specified FacetField is configured; if not, FacetField is configured as hierarchical and multivalued.
        /// </summary>
        /// <param name="facetsConfig">The facets configuration.</param>
        /// <param name="facetField">The facet field.</param>
        public static void EnsureConfig(this FacetsConfig facetsConfig, FacetField facetField)
        {
            if (facetsConfig == null)
                throw new ArgumentNullException(nameof(facetsConfig));
            if (facetField == null)
                throw new ArgumentNullException(nameof(facetField));

            var facetName = facetField.Dim;

            if (!facetsConfig.Contains(facetName))
            {
                // Configure the FacetField if not already configured.
                // By default set it to hierarchical, and multi-valued.
                lock (facetsConfig)
                {
                    if (!facetsConfig.Contains(facetName))
                    {
                        facetsConfig.SetHierarchical(facetName, true);
                        facetsConfig.SetMultiValued(facetName, true);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the FacetResults from FacetCounts.
        /// </summary>
        /// <param name="facetCounts">The facet counts.</param>
        /// <param name="topNCategories">The top n categories.</param>
        /// <returns></returns>
        public static IEnumerable<FacetResult> GetFacets(this FastTaxonomyFacetCounts facetCounts, int topNCategories)
        {
            if (facetCounts == null)
                throw new ArgumentNullException(nameof(facetCounts));
            if (topNCategories <= 0)
                throw new ArgumentException($"{nameof(topNCategories)} cannot be zero or less.");

            var facetResults = new List<FacetResult>();
            var allDims = facetCounts.GetAllDims(topNCategories);

            for (var i = 0; i < allDims.size(); i++)
            {
                var fc = allDims.get(i) as FacetResult;
                if (fc != null)
                    facetResults.Add(fc);
            }

            return facetResults;
        }

        /// <summary>
        /// Converts the specified comma-separated list of category strings to a list of FacetFields.
        /// </summary>
        /// <param name="categoriesCsvString">The comma-separated list of categories.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<FacetField> ToFacetFields(this string categoriesCsvString)
        {
            var facetFields = new List<FacetField>();

            if (!String.IsNullOrWhiteSpace(categoriesCsvString))
            {
                const string COMMA = ",";
                const string ESCAPED_COMMA = @"\,";
                const string ESCAPED_COMMA_TEMP_TOKEN = @"<<££££££>>";

                var categoriesList = categoriesCsvString.Trim()
                                                        .Replace(ESCAPED_COMMA, ESCAPED_COMMA_TEMP_TOKEN)
                                                        .Split(new[] { COMMA }, StringSplitOptions.None);
                if (categoriesList != null)
                {
                    foreach (var categoryString in categoriesList)
                    {
                        if (!String.IsNullOrWhiteSpace(categoryString))
                        {
                            var category = categoryString.Trim().Replace(ESCAPED_COMMA_TEMP_TOKEN, COMMA);
                            var facetField = category.ToLuceneFacetField();
                            if (facetField != null)
                                facetFields.Add(facetField);
                        }
                    }
                }
            }

            return facetFields;
        }

        /// <summary>
        /// Gets the Categories from the FacetCounts.
        /// </summary>
        /// <param name="facetCounts">The facet counts.</param>
        /// <param name="topNCategories">The top n categories.</param>
        /// <returns></returns>
        public static IEnumerable<Category> GetCategories(this FastTaxonomyFacetCounts facetCounts, int topNCategories)
        {
            if (facetCounts == null)
                throw new ArgumentNullException(nameof(facetCounts));
            if (topNCategories <= 0)
                throw new ArgumentException($"{nameof(topNCategories)} cannot be zero or less.");

            var categories = new List<Category>();
            var allDims = facetCounts.GetAllDims(topNCategories);

            for (var i = 0; i < allDims.size(); i++)
            {
                var fc = allDims.get(i) as FacetResult;
                if (fc != null)
                    categories.Add(fc.ToCategory());
            }

            return categories;
        }

        /// <summary>
        /// Converts the FacetResult object to a <see cref="Category"/> object.
        /// </summary>
        /// <param name="facetResult">The FacetResult object.</param>
        /// <returns></returns>
        public static Category ToCategory(this FacetResult facetResult)
        {
            if (facetResult == null)
                throw new ArgumentNullException(nameof(facetResult));

            var category = new Category
            {
                Name = facetResult.Dim,
                Count = facetResult.Value.intValue()
            };

            if (facetResult.ChildCount > 0)
            {
                category.SubCategories = new List<Category>();
                foreach (var value in facetResult.LabelValues)
                    category.SubCategories.Add(new Category { Name = value.Label, Count = value.Value.intValue() });
            }

            return category;
        }

        /// <summary>
        /// Gets the Categories from the Facets object.
        /// </summary>
        /// <param name="facets">The Facets.</param>
        /// <param name="topNCategories">The top N categories to extract.</param>
        /// <param name="selectedFacets">The selected facets.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException">{nameof(topNCategories)}</exception>
        public static IEnumerable<Category> GetCategories(this Facets facets, int topNCategories, IEnumerable<FacetField> selectedFacets)
        {
            if (facets == null)
                throw new ArgumentException(nameof(facets));
            if (topNCategories <= 0)
                throw new ArgumentException($"{nameof(topNCategories)} cannot be zero or less.");
            if (selectedFacets == null)
                throw new ArgumentException(nameof(selectedFacets));

            var categories = new List<Category>();
            var allDims = facets.GetAllDims(topNCategories);

            for (var i = 0; i < allDims.size(); i++)
            {
                var fc = allDims.get(i) as FacetResult;
                if (fc != null)
                {
                    var category = fc.ToCategory();
                    var selectedFacetMatch = selectedFacets.FirstOrDefault(f => f.Dim == category.Name);
                    if (selectedFacetMatch != null)
                    {
                        // Get the sub categories
                    }

                    categories.Add(category);
                }
            }

            return categories;
        }        
    }
}
