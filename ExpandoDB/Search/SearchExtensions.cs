using FlexLucene.Document;
using System;
using LuceneDocument = FlexLucene.Document.Document;

namespace ExpandoDB.Search
{
    public static class SearchExtensions
    {
        public static LuceneDocument ToLuceneDocument(this Content content, IndexSchema indexSchema = null)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            if (indexSchema == null)
                indexSchema = IndexSchema.CreateDefault();

            var dictionary = content.AsDictionary();
            if (!dictionary.ContainsKey(Content.ID_FIELD_NAME))
                throw new InvalidOperationException("Cannot index a Content that does not have an _id.");

            var luceneDocument = new LuceneDocument();
            foreach (var fieldName in dictionary.Keys)
            {
                // NOTE: If schema.IsAutoPopulated is true, auto-add fields to the indexschema

                IndexedField indexedField = null;
                if (!indexSchema.IndexedFields.TryGetValue(fieldName, out indexedField))                
                {
                    indexedField = new IndexedField { 
                        Name = fieldName                        
                    };
                    indexSchema.IndexedFields.TryAdd(fieldName, indexedField);
                }

                var fieldValue = dictionary[fieldName];
                
                var luceneFields = fieldValue.ToLuceneFields(indexedField);
                foreach (var luceneField in luceneFields)
                    luceneDocument.Add(luceneField);
            }

            // The full-text field is always generated and added to the lucene document,
            // even though it is not part of the index schema exposed to the user.
            var fullText = content.ToFullTextString();
             luceneDocument.Add(new TextField(LuceneField.FULL_TEXT_FIELD_NAME, fullText, Field.Store.NO));            

            return luceneDocument;
        }      
        
    }
}
