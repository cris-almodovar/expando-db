namespace ExpandoDB.Rest.DTO
{
    public class UpdateResponseDto : ResponseDto
    {        
        public string from { get; set; }
        public int affectedCount { get; set; }
    }
}
