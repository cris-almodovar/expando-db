﻿using System.Collections.Generic;

namespace ExpandoDB.Rest.DTO
{
    /// <summary>
    /// Represents the JSON data returned by the GET /db/_schemas/{collection} API.
    /// </summary>
    /// <seealso cref="ExpandoDB.Rest.DTO.ResponseDto" />
    public class SchemaResponseDto : ResponseDto
    {          
        public ContentCollectionSchema schema { get; set; }
    }

    /// <summary>
    /// Represents the JSON data returned by the GET /db/_schemas API.
    /// </summary>
    /// <seealso cref="ExpandoDB.Rest.DTO.ResponseDto" />
    public class DatabaseSchemaResponseDto : ResponseDto
    {
        public List<ContentCollectionSchema> schemas { get; set; }
    }
}