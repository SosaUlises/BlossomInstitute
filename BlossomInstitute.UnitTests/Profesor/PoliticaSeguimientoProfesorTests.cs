using BlossomInstitute.Application.DataBase.Profesor;
using FluentAssertions;
using Xunit;

namespace BlossomInstitute.UnitTests.Profesor
{
    public class PoliticaSeguimientoProfesorTests
    {
        [Theory]
        [InlineData(10, 5)]
        [InlineData(6, 3)]
        [InlineData(2, 3)]
        [InlineData(0, 3)]
        public void ObtenerUmbralCorreccionesPendientes_DevuelveUmbralEsperado(
            int cantidadAlumnos,
            int umbralEsperado)
        {
            var umbral = PoliticaSeguimientoProfesor.ObtenerUmbralCorreccionesPendientes(cantidadAlumnos);

            umbral.Should().Be(umbralEsperado);
        }

        [Theory]
        [InlineData(1, 10, false)]
        [InlineData(3, 6, true)]
        [InlineData(6, 10, true)]
        [InlineData(2, 2, false)]
        [InlineData(3, 2, true)]
        public void TieneCorreccionesPendientesRelevantes_AplicaUmbralInstitucional(
            int cantidadCorreccionesPendientes,
            int cantidadAlumnos,
            bool esperado)
        {
            var esRelevante = PoliticaSeguimientoProfesor.TieneCorreccionesPendientesRelevantes(
                cantidadCorreccionesPendientes,
                cantidadAlumnos);

            esRelevante.Should().Be(esperado);
        }
    }
}
