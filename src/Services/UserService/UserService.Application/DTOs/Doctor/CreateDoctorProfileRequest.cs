namespace UserService.Application.DTOs.Doctor
{
    public class CreateDoctorProfileRequest
    {
        public string Specialization { get; set; } = string.Empty;
        public string DiplomaNumber { get; set; } = string.Empty;
        public string? DiplomaSeries { get; set; }
        public string CertificateNumber { get; set; } = string.Empty;
        public DateTime CertificateExpiryDate { get; set; }
        public Guid? OrganizationId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? AcademicDegree { get; set; }
        public string? Biography { get; set; }
    }
}