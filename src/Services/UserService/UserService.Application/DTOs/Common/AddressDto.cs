namespace UserService.Application.DTOs.Common
{
    public class AddressDto
    {
        public string? PostCode { get; set; }
        public string? Country { get; set; }
        public string? Region { get; set; }
        public string? City { get; set; }
        public string? Area { get; set; }
        public string? Street { get; set; }
        public string? House { get; set; }
        public string? Flat { get; set; }
    }
}