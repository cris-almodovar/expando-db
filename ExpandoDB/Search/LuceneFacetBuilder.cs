﻿using FlexLucene.Facet;
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
        private readonly TaxonomyWriter _taxonomyWriter;

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
        /// Creates Lucene FacetFields from the _categories field of the specified Document.
        /// </summary>
        /// <param name="document">The Document.</param>
        /// <param name="schema">The schema of the Document.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        private IEnumerable<FacetField> CreateFacetFields(Document document, Schema schema)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            var facetFields = new List<FacetField>();

            Schema.Field categoriesField = null;
            if (schema.Fields.TryGetValue(Schema.StandardField.CATEGORIES, out categoriesField))
            {
                if (categoriesField.DataType == Schema.DataType.Array &&
                    categoriesField.ArrayElementDataType == Schema.DataType.Text)
                {                    
                    var categories = document.AsDictionary()[Schema.StandardField.CATEGORIES] as IEnumerable<object>;
                    if (categories != null)
                    {
                        // Parse each category to create a FacetField, then add to facetFields list.
                        foreach (var category in categories.Select(c => c as string).Where(c => !String.IsNullOrWhiteSpace(c)).Distinct())
                        {
                            var facetField = category.ToLuceneFacetField();
                            if (facetField != null)
                            {
                                facetFields.Add(facetField);                                
                                FacetsConfig.EnsureConfig(facetField);                                
                            }
                        }
                    }
                } 
                else
                {
                    throw new SchemaException($"The {Schema.StandardField.CATEGORIES} field must be list of Text values.");
                }               
            }

            return facetFields;
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
