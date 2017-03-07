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
    internal static class PatchOperationUtils
    {
        /// <summary>
        /// Validates the specified list of PATCH operations.
        /// </summary>
        /// <param name="operations">The operations.</param>
        /// <exception cref="System.InvalidOperationException">The PATCH request is empty</exception>
        public static void Validate(this IList<PatchOperationDto> operations)
        {
            if (operations == null || operations.Count == 0)
                throw new InvalidOperationException("The PATCH request is empty");

            foreach (var op in operations)
                op.Validate();
        }

        /// <summary>
        /// Validates the specified PATCH operation.
        /// </summary>
        /// <param name="operation">The operation.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.InvalidOperationException">
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// </exception>
        public static void Validate(this PatchOperationDto operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));            

            switch (operation.op)
            {
                case PatchOpCode.ADD:
                case PatchOpCode.REPLACE:
                case PatchOpCode.REMOVE:
                    if (String.IsNullOrWhiteSpace(operation.path))
                        throw new InvalidOperationException($"Invalid parameters for PATCH '{operation.op}' operation.");                    
                    break;

                default:
                    throw new InvalidOperationException($"PATCH operation '{operation.op}' is not supported.");
            }            

            if (!operation.path.StartsWith("/", StringComparison.InvariantCulture))
                throw new ArgumentException($"Invalid path for PATCH '{operation.op}' operation.");

            var pathDepth = operation.path.Count(c => c == '/');            
            if (pathDepth == 1)
            {
                // If we are modifying a top-level field, make sure it's not one of the metadata fields.
                var fieldName = operation.path.TrimStart('/');
                if (fieldName == Schema.MetadataField.ID || 
                    fieldName == Schema.MetadataField.CREATED_TIMESTAMP ||
                    fieldName == Schema.MetadataField.MODIFIED_TIMESTAMP ||
                    fieldName == Schema.MetadataField.FULL_TEXT )
                    throw new ArgumentException($"Cannot modify field '{fieldName}'.");
            }
        }

        /// <summary>
        /// Applies the PATH operation to the given Document.
        /// </summary>
        /// <param name="operation">The PATCH operation.</param>
        /// <param name="document">The Document.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static void Apply(this PatchOperationDto operation, Document document)
        {
            operation.Validate();

            if (document == null)
                throw new ArgumentNullException(nameof(document));            

            var dictionary = document.AsDictionary();
            var path = new JsonPath(operation.path);

            operation.Apply(dictionary, path);
        }        

        private static void Apply(this PatchOperationDto operation, object parentObject, JsonPath path)
        {
            if (parentObject == null)
                throw new ArgumentNullException(nameof(parentObject));
            if (path == null)
                throw new ArgumentNullException(nameof(path));
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
