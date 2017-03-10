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
        /// Converts the Facet value string (e.g. Author:Crispin) to a Lucene <see cref="FacetField" /> object.
        /// </summary>
        /// <param name="facetValueString">The Facet value string.</param>
        /// <param name="facetSettings">The Facet settings field.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">facetValueString</exception>
        /// <exception cref="SchemaException">
        /// </exception>
        public static FacetField ToLuceneFacetField(this string facetValueString, Schema.FacetSettings facetSettings = null)
        {
            const string COLON = ":";
            const string ESCAPED_COLON = @"\:";
            const string ESCAPED_COLON_TEMP_TOKEN = @"<<€€€€€€>>";
            const string SLASH = @"/";
            const string ESCAPED_SLASH = @"\/";
            const string ESCAPED_SLASH_TEMP_TOKEN = @"<<$$$$$$>>";

            //-----------------------------------------------------------------------------------------------------
            // * A Facet value string must have this format => {name}:{value}
            //
            // * Sample Facet value strings:  
            //
            //    "Author:Arthur Dent" => Facet name is "Author", value is "Arthur Dent"
            //    "Publish Date:2013/Mar/12" => Facet name is "Publish Date", value (hierarchical) is "2013/Mar/12"
            //
            // * If either the Facet name or the Facet value includes ":" or "/", it must be escaped with "\". 
            // 
            //-----------------------------------------------------------------------------------------------------

            if (String.IsNullOrWhiteSpace(facetValueString))
                throw new ArgumentNullException(nameof(facetValueString));

            // Convert \/ to <<$$$$$$>> and \: to <<€€€€€€>>
            var facetValueStringCopy = facetValueString.Trim()
                                                       .Replace(ESCAPED_SLASH, ESCAPED_SLASH_TEMP_TOKEN)
                                                       .Replace(ESCAPED_COLON, ESCAPED_COLON_TEMP_TOKEN);

            // Split the string using ":" to get the Facet name
            var facetName = facetValueStringCopy.Contains(COLON) ?
                            facetValueStringCopy.Split(new[] { COLON }, StringSplitOptions.None).FirstOrDefault() :
                            null;

            if (String.IsNullOrWhiteSpace(facetName))
                throw new SchemaException($"Invalid Facet value string: '{facetValueString}'");

            facetName = facetName.Trim()
                                 .Replace(ESCAPED_SLASH_TEMP_TOKEN, SLASH)
                                 .Replace(ESCAPED_COLON, COLON);

            var facetValueStartIndex = facetValueStringCopy.IndexOf(COLON, StringComparison.InvariantCulture) + 1;
            facetValueStringCopy = facetValueStringCopy.Substring(facetValueStartIndex);

            // Split the string using / as separateor to get the facet values
            var categoryParts = facetValueStringCopy.Split(new[] { SLASH }, StringSplitOptions.None);
            if (categoryParts.Count() >= 1)
            {
                var facetValues = categoryParts.Select(s => s.Trim()
                                                             .Replace(ESCAPED_SLASH_TEMP_TOKEN, SLASH)
                                                             .Replace(ESCAPED_COLON_TEMP_TOKEN, COLON))
                                               .ToArray();

                if (facetValues.Any(s => String.IsNullOrWhiteSpace(s)))
                    throw new SchemaException($"Invalid category string: '{facetValueString}'");

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
        /// Converts the <see cref="FastTaxonomyFacetCounts"/> object to a sequence of <see cref="FacetValue"/> objects.
        /// </summary>
        /// <param name="facetCounts">The <see cref="FastTaxonomyFacetCounts"/> object to convert.</param>
        /// <param name="topNFacets">Specifies the number of Facet values to return.</param>
        /// <returns></returns>
        public static IEnumerable<FacetValue> ToFacetValues(this FastTaxonomyFacetCounts facetCounts, int topNFacets)
        {
            if (facetCounts == null)
                throw new ArgumentNullException(nameof(facetCounts));
            if (topNFacets <= 0)
                throw new ArgumentException($"{nameof(topNFacets)} cannot be zero or less.");

            var facetValues = new List<FacetValue>();
            var allDims = facetCounts.GetAllDims(topNFacets);

            for (var i = 0; i < allDims.size(); i++)
            {
                var fc = allDims.get(i) as FacetResult;
                if (fc != null)
                    facetValues.Add(fc.ToFacetValue());
            }

            return facetValues;
        }

        /// <summary>
        /// Converts the FacetResult object to a <see cref="FacetValue"/> object.
        /// </summary>
        /// <param name="facetResult">The FacetResult object.</param>
        /// <returns></returns>
        public static FacetValue ToFacetValue(this FacetResult facetResult)
        {
            if (facetResult == null)
                throw new ArgumentNullException(nameof(facetResult));

            var category = new FacetValue
            {
                Name = facetResult.Dim,
                Count = facetResult.Value.intValue()
            };

            if (category.Count < 0)
                category.Count = null;

            if (facetResult.ChildCount > 0)
            {
                category.Values = new List<FacetValue>();
                foreach (var value in facetResult.LabelValues)
                    category.Values.Add(new FacetValue { Name = value.Label, Count = value.Value.intValue() });

                category.Count = category.Values.Sum(sc => sc.Count);
            }

            return category;
        }

        /// <summary>
        /// Converts the <see cref="Facets"/> object to a sequence of <see cref="FacetValue"/> objects.
        /// </summary>
        /// <param name="facets">The Facets object.</param>
        /// <param name="topNFacets">Specifies the number of Facets to return.</param>
        /// <param name="selectedFacets">The selected Facets.</param>
        /// <returns></returns>        
        public static IEnumerable<FacetValue> ToFacetValues(this Facets facets, int topNFacets, IEnumerable<FacetField> selectedFacets)
        {
            if (facets == null)
                throw new ArgumentException(nameof(facets));
            if (topNFacets <= 0)
                throw new ArgumentException($"{nameof(topNFacets)} cannot be zero or less.");
            if (selectedFacets == null)
                throw new ArgumentException(nameof(selectedFacets));

            var facetValues = new List<FacetValue>();
            var allDimensions = facets.GetAllDims(topNFacets);

            for (var i = 0; i < allDimensions.size(); i++)
            {
                // Get the Facet (category) names
                var facetResult = allDimensions.get(i) as FacetResult;
                if (facetResult != null)
                {
                    var facetValue = facetResult.ToFacetValue();

                    // Check if the current Facet is one of the Facets 
                    // that the user wants to drill-down to.                    

                    var facetToDrillDownTo = selectedFacets.FirstOrDefault(f => f.Dim == facetValue.Name);
                    if (facetToDrillDownTo != null &&
                        (facetToDrillDownTo.Path?.Length ?? 0) > 0)
                    {
                        // If yes, then we want to get the names and counts of all category child values.
                        // Do this by traversing the facet path.

                        var currentFacet = facetValue;
                        for (var j = 0; j < facetToDrillDownTo.Path.Length; j++)
                        {
                            var currentFacetPathSegments = facetToDrillDownTo.Path.Take(j + 1).ToArray();
                            var childFacetResult = facets.GetTopChildren(topNFacets, facetToDrillDownTo.Dim, currentFacetPathSegments);
                            if (childFacetResult == null)
                                break;

                            if (childFacetResult.ChildCount > 0)
                            {
                                var childName = currentFacetPathSegments.Last();
                                if (currentFacet.Values == null)
                                    currentFacet.Values = new List<FacetValue>();

                                var childFacet = currentFacet.Values.FirstOrDefault(c => c.Name == childName);
                                if (childFacet == null)
                                {
                                    childFacet = new FacetValue { Name = childName };
                                    currentFacet.Values.Add(childFacet);
                                }

                                if (childFacet.Values == null)
                                    childFacet.Values = new List<FacetValue>();

                                foreach (var lv in childFacetResult.LabelValues)
                                    childFacet.Values.Add(new FacetValue { Name = lv.Label, Count = lv.Value.intValue() });

                                childFacet.Count = childFacet.Values.Sum(sc => sc.Count);

                                currentFacet = childFacet;
                            }
                        }
                    }

                    facetValues.Add(facetValue);
                }
            }

            return facetValues;
        }
    }
}
