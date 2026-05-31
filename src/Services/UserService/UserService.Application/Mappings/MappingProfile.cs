using AutoMapper;
using UserService.Application.DTOs.Common;
using UserService.Domain.Entities;
using Profile = AutoMapper.Profile;

namespace UserService.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<AddressDto, Address>().ReverseMap();

            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Sex, opt => opt.MapFrom(src => src.Sex.ToString()));
        }
    }
}