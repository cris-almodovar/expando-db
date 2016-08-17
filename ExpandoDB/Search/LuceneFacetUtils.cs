using FlexLucene.Facet;
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
            if (categoryParts.Length > 1)
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
    }
}
