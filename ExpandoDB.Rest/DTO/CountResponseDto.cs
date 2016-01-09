namespace ExpandoDB.Rest.DTO
{
    public class CountResponseDto : ResponseDto
    {          
        public string from { get; set; }
        public string where { get; set; }
        public int count { get; set; }        
    }
}
