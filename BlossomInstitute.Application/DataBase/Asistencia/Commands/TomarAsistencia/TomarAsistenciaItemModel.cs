using BlossomInstitute.Domain.Entidades.Clase;

namespace BlossomInstitute.Application.DataBase.Asistencia.Commands.TomarAsistencia
{
    public class TomarAsistenciaItemModel
    {
        public int AlumnoId { get; set; }
        public EstadoAsistencia Estado { get; set; }
    }
}
