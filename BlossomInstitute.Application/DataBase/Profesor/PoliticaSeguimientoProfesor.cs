namespace BlossomInstitute.Application.DataBase.Profesor
{
    public static class PoliticaSeguimientoProfesor
    {
        private const int MinimoCorreccionesPendientesRelevantes = 3;

        public static int ObtenerUmbralCorreccionesPendientes(int cantidadAlumnos)
        {
            if (cantidadAlumnos <= 0)
                return MinimoCorreccionesPendientesRelevantes;

            return Math.Max(
                MinimoCorreccionesPendientesRelevantes,
                (int)Math.Ceiling(cantidadAlumnos * 0.5m));
        }

        public static bool TieneCorreccionesPendientesRelevantes(
            int cantidadCorreccionesPendientes,
            int cantidadAlumnos)
        {
            return cantidadCorreccionesPendientes >= ObtenerUmbralCorreccionesPendientes(cantidadAlumnos);
        }
    }
}
