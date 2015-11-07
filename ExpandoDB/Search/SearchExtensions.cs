using FlexLucene.Document;
using System;
using LuceneDocument = FlexLucene.Document.Document;

namespace ExpandoDB.Search
{
    public static class SearchExtensions
    {
        public static LuceneDocument ToLuceneDocument(this Content content, SearchSchema searchSchema = null)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            if (searchSchema == null)
                searchSchema = SearchSchema.Default;

            var dictionary = content.AsDictionary();
            if (!dictionary.ContainsKey(Content.ID_FIELD_NAME))
                throw new InvalidOperationException("Cannot index a Content that does not have an _id.");

            var luceneDocument = new LuceneDocument();
            foreach (var fieldName in dictionary.Keys)
            {
                IndexedField indexedField = null;
                if (!searchSchema.IndexedFields.TryGetValue(fieldName, out indexedField))                
                {
                    indexedField = new IndexedField { 
                        Name = fieldName                        
                    };
                    searchSchema.IndexedFields.TryAdd(fieldName, indexedField);
                }

                var fieldValue = dictionary[fieldName];
                
                var luceneFields = fieldValue.ToLuceneFields(indexedField);
                foreach (var luceneField in luceneFields)
                    luceneDocument.Add(luceneField);
            }

            // Add the full text field            
            var fullTextField = new Field(LuceneField.FULL_TEXT_FIELD_NAME, content.ToFullTextString(), LuceneField.FULL_TEXT_FIELD_TYPE);
            luceneDocument.Add(fullTextField);

            return luceneDocument;
        }      
        
    }
}
