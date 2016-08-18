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
            const string SLASH = @"/";
            const string ESCAPED_SLASH = @"\/";
            const string ESCAPED_SLASH_TEMP_TOKEN = @"<<$$$$$$>>";
            
            // Sample categoryString:  "Author\Arthur Dent" => facet name is "Author", value is "Crispin"

            if (String.IsNullOrWhiteSpace(categoryString))
                throw new ArgumentNullException(nameof(categoryString));

            // Convert \/ to $$$$$$
            var categoryStringCopy = categoryString.Trim().Replace(ESCAPED_SLASH, ESCAPED_SLASH_TEMP_TOKEN);

            // Split the string using / as separateor
            var categoryParts = categoryStringCopy.Split(new[] { SLASH }, StringSplitOptions.None);
            if (categoryParts.Count() > 1)
            {
                // The first part is the facet name
                var facetName = categoryParts.FirstOrDefault();
                if (String.IsNullOrWhiteSpace(facetName))
                    throw new SchemaException($"Invalid category string: '{categoryStringCopy}'");

                facetName = facetName.Trim().Replace(ESCAPED_SLASH_TEMP_TOKEN, SLASH);

                // The remaing parts the values of the facet
                var facetValues = categoryParts.Skip(1)
                                               .Take(categoryParts.Length - 1)
                                               .Select(s => s.Trim().Replace(ESCAPED_SLASH_TEMP_TOKEN, SLASH))
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
        /// Converts the FacetResult object to a <see cref="Category"/> object.
        /// </summary>
        /// <param name="facetResult">The FacetResult object.</param>
        /// <returns></returns>
        public static Category ToCategory(this FacetResult facetResult)
        {
            if (facetResult == null)
                throw new ArgumentNullException(nameof(facetResult));

            var category = new Category { Name = facetResult.Dim };
            foreach (var value in facetResult.LabelValues)
                category.Values.Add(new Category.CategoryCount { Name = value.Label, Count = value.Value.intValue() });            

            return category;
        }

        /// <summary>
        /// Adds the selected categories to the DrillDownQuery.
        /// </summary>
        /// <param name="drillDownQuery">The drill down query.</param>
        /// <param name="categoriesCsvString">The comma-separated list of categories.</param>
        public static void AddSelectedCategories(this DrillDownQuery drillDownQuery, string categoriesCsvString)
        {
            if (drillDownQuery == null)
                throw new ArgumentNullException(nameof(drillDownQuery));
            if (String.IsNullOrWhiteSpace(categoriesCsvString))
                return;

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
                            drillDownQuery.Add(facetField.Dim, facetField.Path);                        
                    }
                }
            }
        }

        /// <summary>
        /// Gets the FacetResults from DrillSidewaysResult.
        /// </summary>
        /// <param name="drillSidewaysResult">The DrillSidewaysResult.</param>
        /// <param name="topNCategories">The top N categories to extract.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static IEnumerable<FacetResult> GetFacets(this DrillSidewaysDrillSidewaysResult drillSidewaysResult, int topNCategories)
        {
            if (drillSidewaysResult == null)
                throw new ArgumentNullException(nameof(drillSidewaysResult));
            if (topNCategories <= 0)
                throw new ArgumentException($"{nameof(topNCategories)} cannot be zero or less.");

            var facetResults = new List<FacetResult>();
            var allDims = drillSidewaysResult.Facets.GetAllDims(topNCategories);

            for (var i = 0; i < allDims.size(); i++)
            {
                var fc = allDims.get(i) as FacetResult;
                if (fc != null)
                    facetResults.Add(fc);
            }

            return facetResults;

        }
    }
}
