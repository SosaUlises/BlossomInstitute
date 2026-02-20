using AutoMapper;
using BlossomInstitute.Application.DataBase.Profesor.Queries.GetAllProfesores;
using BlossomInstitute.Domain.Entidades.Usuario;

namespace BlossomInstitute.Application.Configuration
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            // Profesor
            CreateMap<UsuarioEntity, GetAllProfesoresModel>()
              .ForMember(d => d.Telefono, opt => opt.MapFrom(s => s.PhoneNumber));
        }
    }
}
