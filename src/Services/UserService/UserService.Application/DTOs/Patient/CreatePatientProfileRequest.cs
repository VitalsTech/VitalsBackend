using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UserService.Application.DTOs.Common;

namespace UserService.Application.DTOs.Patient
{
    public class CreatePatientProfileRequest
    {
        public string? InsuranceNumber { get; set; }
        public string? SNILS { get; set; }
        public string? BloodType { get; set; }
        public string? Allergies { get; set; }
        public AddressDto? ResidenceAddress { get; set; }
        public AddressDto? RegistrationAddress { get; set; }
    }
}
