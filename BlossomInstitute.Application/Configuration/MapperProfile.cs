using AutoMapper;
using BlossomInstitute.Application.DataBase.Alumno.Queries.GetAll;
using BlossomInstitute.Application.DataBase.Profesor.Queries.GetAll;
using BlossomInstitute.Domain.Entidades.Usuario;

namespace BlossomInstitute.Application.Configuration
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            // Profesor
            CreateMap<UsuarioEntity, GetProfesorModel>()
              .ForMember(d => d.Telefono, opt => opt.MapFrom(s => s.PhoneNumber));

            // Alumno
            CreateMap<UsuarioEntity, GetAlumnoModel>()
              .ForMember(d => d.Telefono, opt => opt.MapFrom(s => s.PhoneNumber));
        }
    }
}
