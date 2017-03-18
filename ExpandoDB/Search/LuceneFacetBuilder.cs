using Common.Logging;
using FlexLucene.Facet;
using FlexLucene.Facet.Taxonomy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LuceneDocument = FlexLucene.Document.Document;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Creates Lucene facets by reading the _categories field of a Document
    /// and converting each element to a FacetField.
    /// </summary>    
    public class LuceneFacetBuilder
    {
        private const int MAX_FACET_TEXT_LENGTH = 100;
        private readonly TaxonomyWriter _taxonomyWriter;
        private readonly ILog _log = LogManager.GetLogger(nameof(LuceneFacetBuilder));  

        /// <summary>
        /// Gets the facets configuration.
        /// </summary>
        /// <value>
        /// The facets configuration.
        /// </value>
        public FacetsConfig FacetsConfig { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LuceneFacetBuilder"/> class.
        /// </summary>
        /// <param name="taxonomyWriter">The taxonomy writer.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public LuceneFacetBuilder(TaxonomyWriter taxonomyWriter)
        {
            if (taxonomyWriter == null)
                throw new ArgumentNullException(nameof(taxonomyWriter));

            _taxonomyWriter = taxonomyWriter;
            FacetsConfig = new FacetsConfig();                        
        }

        /// <summary>
        /// Creates Lucene FacetFields from Document Fields that are configured as Facets.
        /// </summary>
        /// <param name="document">The Document.</param>
        /// <param name="schema">The schema of the Document.</param>
        /// <returns></returns>       
        private IEnumerable<FacetField> CreateFacetFields(Document document, Schema schema)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            // We will automatically create FacetFields for Document fields that have a non-null FacetSetting property.
            // Facet creation is only supported for top-level fields with a valid FacetSettings.

            var facetFields = new List<FacetField>();
                        
            var fieldsToCreateFacets = schema.Fields.Values.Where(item => item.IsTopLevel && item.IsFacet)?.ToList();
            if (fieldsToCreateFacets?.Count > 0)
            {
                foreach (var schemaField in fieldsToCreateFacets)
                {
                    var facetSettings = schemaField.FacetSettings;
                    var fieldValue = document[schemaField.Name];

                    if (schemaField.DataType == Schema.DataType.Array)
                    {
                        var arrayItems = fieldValue as IEnumerable<object>;
                        if (arrayItems?.Count() > 0)
                        {
                            foreach (var item in arrayItems)
                            {
                                var facetField = CreateFacetField(item, schemaField.ArrayElementDataType, facetSettings);
                                if (facetField != null)
                                {
                                    facetFields.Add(facetField);
                                    FacetsConfig.EnsureConfig(facetField, facetSettings);
                                }
                            }
                        }                        
                    }
                    else
                    {
                        var facetField = CreateFacetField(fieldValue, schemaField.DataType, facetSettings);
                        if (facetField != null)
                        {
                            facetFields.Add(facetField);
                            FacetsConfig.EnsureConfig(facetField, facetSettings);
                        }
                    }                    
                }
            }             

            
            return facetFields;
        }


        /// <summary>
        /// Creates a Lucene FacetField object from the field value.
        /// </summary>
        /// <param name="fieldValue">The field value.</param>
        /// <param name="dataType">Type of the data.</param>
        /// <param name="facetSettings">The facet settings.</param>
        /// <returns></returns>
        private FacetField CreateFacetField(object fieldValue, Schema.DataType dataType, Schema.FacetSettings facetSettings)
        {
            FacetField facetField = null;                        
            var facetName = facetSettings.FacetName;

            if (String.IsNullOrWhiteSpace(facetName))
                return null;
            if (fieldValue == null)
                return null;

            switch (dataType)
            {
                case Schema.DataType.Text:
                    var textValue = fieldValue as string;
                    if (textValue?.Length > MAX_FACET_TEXT_LENGTH)
                    {
                        _log.Warn($"The length of the value of Facet '{facetName}' is too long. Max length is {MAX_FACET_TEXT_LENGTH}");
                    }
                    else
                    {
                        if (!String.IsNullOrWhiteSpace(textValue))
                            facetField = $"{facetName}:{textValue}".ToLuceneFacetField(facetSettings);
                    }
                    break;

                case Schema.DataType.DateTime:
                    var dateFormat = facetSettings.FormatString ?? "yyyy/MMM/dd";
                    var dateStringValue = ((DateTime)fieldValue).ToString(dateFormat);
                    if (!String.IsNullOrWhiteSpace(dateStringValue))
                        facetField = $"{facetName}:{dateStringValue}".ToLuceneFacetField(facetSettings);
                    break;

                case Schema.DataType.Boolean:
                    var boolStringValue = ((bool)fieldValue).ToString().ToLower();
                    if (!String.IsNullOrWhiteSpace(boolStringValue))
                        facetField = $"{facetName}:{boolStringValue}".ToLuceneFacetField(facetSettings);
                    break;
                
                case Schema.DataType.Guid:
                    var guidValue = (Guid)fieldValue;                    
                    if (!String.IsNullOrWhiteSpace(facetSettings.FormatString))
                        facetField = $"{facetName}:{guidValue.ToString(facetSettings.FormatString)}".ToLuceneFacetField(facetSettings);
                    else
                        facetField = $"{facetName}:{guidValue.ToString()}".ToLuceneFacetField(facetSettings);

                    break;
                         
                case Schema.DataType.Number:
                    var numberValue = Convert.ToDouble(fieldValue);                                       
                    if (!String.IsNullOrWhiteSpace(facetSettings.FormatString))
                        facetField = $"{facetName}:{numberValue.ToString(facetSettings.FormatString)}".ToLuceneFacetField(facetSettings);
                    else
                        facetField = $"{facetName}:{numberValue.ToString()}".ToLuceneFacetField(facetSettings);
                    break;
                
            }

            return facetField;
        }

        /// <summary>
        /// Rebuilds the LuceneDocument by adding FacetFields, which are generated from the original Document.
        /// </summary>
        /// <param name="luceneDocument">The lucene document.</param>
        /// <param name="document">The document.</param>
        /// <param name="schema">The schema.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public LuceneDocument RebuildDocumentWithFacets(LuceneDocument luceneDocument, Document document, Schema schema)
        {
            if (luceneDocument == null)
                throw new ArgumentNullException(nameof(luceneDocument));
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            var facetFields = CreateFacetFields(document, schema);
            if (facetFields.Count() > 0)
            {
                foreach (var facetField in facetFields)
                    luceneDocument.Add(facetField);

                luceneDocument = FacetsConfig.Build(_taxonomyWriter, luceneDocument);
            }

            return luceneDocument;
        }
    }
}
