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
        const string COLON = ":";
        const string ESCAPED_COLON = @"\:";
        const string ESCAPED_COLON_TEMP_REPLACEMENT_TOKEN = @"<<€€€€€€>>";
        const string SLASH = @"/";

        /// <summary>
        /// Converts a single Facet filter string (e.g. Author:Crispin) to a Lucene <see cref="FacetField" /> object.
        /// </summary>
        /// <param name="facetFilter">The Facet filter string.</param>
        /// <param name="facetSettings">The Facet settings field.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">facetFilter</exception>
        /// <exception cref="SchemaException">
        /// </exception>
        public static FacetField ToLuceneFacetField(this string facetFilter, Schema.FacetSettings facetSettings)
        {            
            var hierarchySeparator = facetSettings?.HierarchySeparator ?? SLASH;
            var escapedHierarchySeparator = $"\\{hierarchySeparator}";
            var escapedHierarchySeparatorTempToken = @"<<$$$$$$>>";

            //-----------------------------------------------------------------------------------------------------
            // * A Facet filter string is a name-value pair that has this format => {name}:{value}
            //
            // * Sample Facet value strings:  
            //
            //    "Author:Arthur Dent" => Facet name is "Author", value is "Arthur Dent"
            //    "Publish Date:2013/Mar/12" => Facet name is "Publish Date", value (hierarchical) is "2013/Mar/12"
            //
            // * If either the Facet name or the Facet value includes ":" or "/", it must be escaped with "\". 
            // 
            //-----------------------------------------------------------------------------------------------------

            if (String.IsNullOrWhiteSpace(facetFilter))
                throw new ArgumentNullException(nameof(facetFilter));

            // Convert any escaped hierarchy separator chars (e.g. \/) to <<$$$$$$>>
            // and any escaped colon chars (i.e. \:) to <<€€€€€€>>
            var facetFilterCopy = facetFilter.Trim()
                                             .Replace(escapedHierarchySeparator, escapedHierarchySeparatorTempToken)
                                             .Replace(ESCAPED_COLON, ESCAPED_COLON_TEMP_REPLACEMENT_TOKEN);

            // Split the string using ":" to get the Facet name
            var facetName = facetFilterCopy.Contains(COLON) ?
                            facetFilterCopy.Split(new[] { COLON }, StringSplitOptions.None).FirstOrDefault() :
                            null;

            if (String.IsNullOrWhiteSpace(facetName))
                throw new SchemaException($"Invalid Facet value string: '{facetFilter}'");

            facetName = facetName.Trim()
                                 .Replace(escapedHierarchySeparatorTempToken, hierarchySeparator)
                                 .Replace(ESCAPED_COLON, COLON);

            var facetValueStartIndex = facetFilterCopy.IndexOf(COLON, StringComparison.InvariantCulture) + 1;
            var facetValue = facetFilterCopy.Substring(facetValueStartIndex);

            FacetField facetField = null;

            if (facetSettings.IsHierarchical)
            {
                // Split the Facet value using the hierarchy separator char as separateor to get the child Facet values.
                var facetValueParts = facetValue.Split(new[] { hierarchySeparator }, StringSplitOptions.None);
                if (facetValueParts.Count() >= 1)
                {
                    var facetValues = facetValueParts.Select(s => s.Trim()
                                                                 .Replace(escapedHierarchySeparatorTempToken, hierarchySeparator)
                                                                 .Replace(ESCAPED_COLON_TEMP_REPLACEMENT_TOKEN, COLON))
                                                   .ToArray();

                    if (facetValues.Any(s => String.IsNullOrWhiteSpace(s)))
                        throw new SchemaException($"Invalid Facet filter string: '{facetFilter}'");

                    facetField = new FacetField(facetName, facetValues);                    
                }
            }
            else
            {
                facetField = new FacetField(facetName, facetValue);
            }

            return facetField;
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
        /// Converts the specified comma-separated list of Facet filter strings to a list of FacetFields;
        /// sample Facet filter string: "Author:Crispin" and "Publish Date:2010" - these can be
        /// concatenated using a comma to produce "Author:Crispin,Publish Date:2010"
        /// </summary>
        /// <param name="facetFilters">The comma-separated list of Facet filter strings.</param>
        /// <param name="schema">The schema.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<FacetField> ToLuceneFacetFields(this string facetFilters, Schema schema)
        {
            var facetFields = new List<FacetField>();

            if (!String.IsNullOrWhiteSpace(facetFilters))
            {
                const string COMMA = ",";
                const string ESCAPED_COMMA = @"\,";
                const string ESCAPED_COMMA_TEMP_TOKEN = @"<<££££££>>";

                var facetFiltersList = facetFilters.Trim()
                                                   .Replace(ESCAPED_COMMA, ESCAPED_COMMA_TEMP_TOKEN)
                                                   .Split(new[] { COMMA }, StringSplitOptions.None)
                                                   ?.ToList();
                if (facetFiltersList?.Count > 0)
                {
                    foreach (var facetFilter in facetFiltersList)
                    {
                        if (!String.IsNullOrWhiteSpace(facetFilter))
                        {
                            var facetValueString = facetFilter.Trim().Replace(ESCAPED_COMMA_TEMP_TOKEN, COMMA);
                            var facetName = facetValueString.Contains(COLON) ?
                                            facetValueString.Split(new[] { COLON }, StringSplitOptions.None).FirstOrDefault() :
                                            null;

                            if (String.IsNullOrWhiteSpace(facetName))
                                throw new SchemaException($"No Facet name specified in: '{facetValueString}'");

                            var facetSettings = GetFacetSettings(facetName, schema);
                            if (facetSettings == null)
                                throw new SchemaException($"Invalid Facet: {facetName}");

                            var facetField = facetValueString.ToLuceneFacetField(facetSettings);
                            if (facetField != null)
                                facetFields.Add(facetField);
                        }
                    }
                }
            }

            return facetFields;
        }

        /// <summary>
        /// Gets the FacetSettings object for the given Facet name.
        /// </summary>
        /// <param name="facetName">Name of the facet.</param>
        /// <param name="schema">The schema.</param>
        /// <returns></returns>
        private static Schema.FacetSettings GetFacetSettings(string facetName, Schema schema)
        {
            Schema.FacetSettings facetSettings = null;

            if (!String.IsNullOrWhiteSpace(facetName))
            {
                facetSettings = (from f in schema.Fields.Values
                                 where f.IsFacet && f.FacetSettings.FacetName == facetName
                                 select f.FacetSettings)
                                .FirstOrDefault();
            }

            return facetSettings;
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
