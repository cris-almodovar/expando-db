using ExpandoDB.Rest.DTO;
using ExpandoDB.Search;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Rest
{
    internal static class PatchOperationExtensions
    {  
        public static void Validate(this IList<PatchOperationDto> operations)
        {
            if (operations == null || operations.Count == 0)
                throw new InvalidOperationException("The PATCH request is empty");

            foreach (var op in operations)
                op.Validate();
        }

        public static void Validate(this PatchOperationDto operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));            

            switch (operation.op)
            {
                case PatchOp.ADD:
                case PatchOp.REPLACE:
                    if (operation.path == null || operation.value == null)
                        throw new InvalidOperationException($"Invalid parameters for PATCH '{operation.op}' operation.");
                    if (operation.path == String.Empty)
                        throw new InvalidOperationException($"Update of whole document currently not supported for PATCH '{operation.op}' operation. Use PUT instead.");
                    break;

                case PatchOp.REMOVE:
                    if (operation.path == null)
                        throw new InvalidOperationException($"Invalid parameters for PATCH '{operation.op}' operation.");
                    break;

                default:
                    throw new InvalidOperationException($"PATCH operation '{operation.op}' is not supported.");
            }            

            if (!operation.path.StartsWith("/", StringComparison.InvariantCulture))
                throw new ArgumentException($"Invalid path for PATCH '{operation.op}' operation.");

            var pathDepth = operation.path.Count(c => c == '/');
            if (pathDepth > 2)
                throw new ArgumentException("Path depth > 2 is currently not supported. If path depth = 2, the second part must be a numeric array index.");

            if (pathDepth == 1)
            {
                // If we are modifying a top-level field, make sure it's not one of the metadata fields.
                var fieldName = operation.path.TrimStart('/');
                if (fieldName == Content.ID_FIELD_NAME || 
                    fieldName == Content.CREATED_TIMESTAMP_FIELD_NAME ||
                    fieldName == Content.MODIFIED_TIMESTAMP_FIELD_NAME ||
                    fieldName == LuceneExtensions.FULL_TEXT_FIELD_NAME )
                    throw new ArgumentException($"Invalid parameters for PATCH '{operation.op}' operation.");
            }
        }

        public static void Apply(this PatchOperationDto operation, Content content)
        {
            operation.Validate();

            if (content == null)
                throw new ArgumentNullException(nameof(content));            

            var dictionary = content.AsDictionary();
            var pathPointer = new JsonPointer(operation.path);

            switch (operation.op)
            {
                case PatchOp.ADD:
                    Add(dictionary, pathPointer, operation.value);
                    break;

                case PatchOp.REPLACE:
                    Replace(dictionary, pathPointer, operation.value);
                    break;

                case PatchOp.REMOVE:
                    Remove(dictionary, pathPointer);
                    break;
            }
        }        

        private static void Add(IDictionary<string, object> dictionary, JsonPointer pathPointer, object value)
        {            
            var fieldName = pathPointer.Parts[0];
            var fieldValue = dictionary.ContainsKey(fieldName) ? dictionary[fieldName] : null;        

            if (!dictionary.ContainsKey(fieldName))
            {
                // The field does not exist, add it.
                dictionary.Add(fieldName, value); 
            }
            else
            {
                // The field exists; check if it's an array.
                if (fieldValue is IList)
                {
                    if (value is IList)
                    {
                        dictionary[fieldName] = value;
                    }
                    else
                    {
                        if (pathPointer.Parts.Count < 2)
                            throw new ArgumentException("PATH path must include an array index parameter.");

                        var array = fieldValue as IList;
                        if (array == null)
                            throw new InvalidOperationException($"Content does not have an array field named '{fieldName}'");

                        var arrayIndexPart = pathPointer.Parts[1];
                        if (arrayIndexPart == "-")
                        {
                            // Append to the array.
                            array.Add(value);
                        }
                        else
                        {
                            var arrayIndex = -1;
                            Int32.TryParse(arrayIndexPart, out arrayIndex);

                            if (arrayIndex < 0 || arrayIndex > array.Count)
                                throw new InvalidOperationException($"Invalid array index for PATCH add operation: '{arrayIndexPart}'");

                            array.Insert(arrayIndex, value);
                        }
                    }
                }
                else
                {
                    // The field is not an array; replace its value.
                    dictionary[fieldName] = value; 
                }
            }
            
        }        

        private static void Replace(IDictionary<string, object> dictionary, JsonPointer pathPointer, object value)
        {
            var fieldName = pathPointer.Parts[0];
            var fieldValue = dictionary.ContainsKey(fieldName) ? dictionary[fieldName] : null;
            if (!dictionary.ContainsKey(fieldName))
                throw new InvalidOperationException($"Content does not have a field named '{fieldName}'");

            // The field exists; check if it's an array.
            if (fieldValue is IList)
            {
                if (value is IList)
                {
                    dictionary[fieldName] = value;
                }
                else
                {
                    if (pathPointer.Parts.Count < 2)
                        throw new ArgumentException("PATH path must include an array index parameter.");

                    var array = fieldValue as IList;
                    if (array == null)
                        throw new InvalidOperationException($"Content does not have an array field named '{fieldName}'");

                    var arrayIndexPart = pathPointer.Parts[1];
                    var arrayIndex = -1;
                    Int32.TryParse(arrayIndexPart, out arrayIndex);

                    if (arrayIndex < 0 || arrayIndex >= array.Count)
                        throw new InvalidOperationException($"Invalid array index for PATCH replace operation: '{arrayIndexPart}'");

                    array[arrayIndex] = value;
                }         
            }
            else
            {
                // The field is not an array; replace its value.
                dictionary[fieldName] = value;
            }
        }

        private static void Remove(IDictionary<string, object> dictionary, JsonPointer pathPointer)
        {
            var fieldName = pathPointer.Parts[0];
            var fieldValue = dictionary.ContainsKey(fieldName) ? dictionary[fieldName] : null;
            if (!dictionary.ContainsKey(fieldName))
                throw new InvalidOperationException($"Content does not have a field named '{fieldName}'");

            // The field exists; check if it's an array.
            if (fieldValue is IList)
            {
                if (pathPointer.Parts.Count == 1)
                {
                    dictionary.Remove(fieldName);
                }
                else
                {
                    if (pathPointer.Parts.Count < 2)
                        throw new ArgumentException("PATH path must include an array index parameter.");

                    var array = fieldValue as IList;
                    if (array == null)
                        throw new InvalidOperationException($"Content does not have an array field named '{fieldName}'");

                    var arrayIndexPart = pathPointer.Parts[1];
                    var arrayIndex = -1;
                    Int32.TryParse(arrayIndexPart, out arrayIndex);

                    if (arrayIndex < 0 || arrayIndex >= array.Count)
                        throw new InvalidOperationException($"Invalid array index for PATCH replace operation: '{arrayIndexPart}'");

                    array.RemoveAt(arrayIndex);
                }
            }
            else
            {
                // The field is not an array; remove it from the dictionary.
                dictionary.Remove(fieldName);
            }
        }       
    }

    internal static class PatchOp
    {
        public const string ADD = "add";
        public const string REMOVE = "remove";
        public const string REPLACE = "replace";
    }

    internal class JsonPointer
    {
        public string Path { get; private set; }
        public IList<string> Parts { get; private set; }

        public JsonPointer(string path)
        {
            if (String.IsNullOrWhiteSpace(path) || !path.StartsWith("/", StringComparison.InvariantCulture))
                throw new ArgumentException(nameof(path));

            Path = path;
            Parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }

}
