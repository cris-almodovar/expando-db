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
                case PatchOpCode.ADD:
                case PatchOpCode.REPLACE:
                    if (operation.path == null || operation.value == null)
                        throw new InvalidOperationException($"Invalid parameters for PATCH '{operation.op}' operation.");
                    if (operation.path == String.Empty)
                        throw new InvalidOperationException($"Update of whole document currently not supported for PATCH '{operation.op}' operation. Use PUT instead.");
                    break;

                case PatchOpCode.REMOVE:
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
            var path = new JsonPath(operation.path);

            operation.Apply(dictionary, path);
        }        

        private static void Apply(this PatchOperationDto operation, object parentObject, JsonPath path)
        {
            if (parentObject == null)
                throw new ArgumentException(nameof(parentObject));
            if (path == null)
                throw new ArgumentException(nameof(path));
            if (path.Segments.Count < 1)
                throw new ArgumentException($"Invalid path: '{path}'");

            var currentSegment = path.Segments[0];            
            if (parentObject is IList)
            {
                var array = parentObject as IList;
                var index = currentSegment.ToArrayIndex(array);                

                if (path.HasNext())
                {
                    // We're not yet on the target location, continue traversing the path.
                    var nextPath = path.GetNext();
                    object childObject = index < array.Count ? array[index] : null;
                    operation.Apply(childObject, nextPath);
                }
                else
                {
                    // We are on the target location, perform the operation.
                    switch (operation.op)
                    {
                        case PatchOpCode.ADD:
                            array.Insert(index, operation.value);
                            break;

                        case PatchOpCode.REPLACE:
                            array[index] = operation.value;
                            break;

                        case PatchOpCode.REMOVE:
                            array.RemoveAt(index);
                            break;
                    }                    
                }
            }
            else if (parentObject is IDictionary<string, object>)
            {
                var dictionary = parentObject as IDictionary<string, object>;
                var fieldName = currentSegment;

                if (path.HasNext())
                {
                    // We're not yet on the target location, continue traversing the path.
                    var nextPath = path.GetNext();
                    object childObject = dictionary.ContainsKey(fieldName) ? dictionary[fieldName] : null;
                    operation.Apply(childObject, nextPath);
                }
                else
                {
                    // We are on the target location, perform the operation.
                    switch (operation.op)
                    {
                        case PatchOpCode.ADD:
                        case PatchOpCode.REPLACE:
                            dictionary[fieldName] = operation.value;
                            break;

                        case PatchOpCode.REMOVE:
                            dictionary.Remove(fieldName);
                            break;
                    }                    
                }
            }               

        }

        private static int ToArrayIndex(this string segment, IList array)
        {
            if (String.IsNullOrWhiteSpace(segment))
                throw new ArgumentException(nameof(segment));
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (segment == "-")
                return array.Count;

            var arrayIndex = -1;
            Int32.TryParse(segment, out arrayIndex);
            if (arrayIndex < 0 || arrayIndex > array.Count)
                throw new InvalidOperationException($"Invalid array index for PATCH add operation: '{segment}'");

            return arrayIndex;
        }             
       
    }

    internal static class PatchOpCode
    {
        public const string ADD = "add";
        public const string REMOVE = "remove";
        public const string REPLACE = "replace";
    }

    internal class JsonPath
    {
        public string Path { get { var joined = String.Join("/", Segments); return joined.Length > 0 ? $"/{joined}" : joined; } }
        public IList<string> Segments { get; private set; }

        public JsonPath(string path)
        {
            if (String.IsNullOrWhiteSpace(path) || !path.StartsWith("/", StringComparison.InvariantCulture))
                throw new ArgumentException(nameof(path));
            
            Segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public override string ToString()
        {
            return Path;
        }

        public bool HasNext()
        {
            return Segments.Count > 1;
        }

        public JsonPath GetNext()
        {
            if (HasNext())
            {
                var newPath = "/" + String.Join("/", Segments.Skip(1));
                return new JsonPath(newPath);
            }
            else
                throw new InvalidOperationException("JsonPath is in the last segment");
        }
    }

}
