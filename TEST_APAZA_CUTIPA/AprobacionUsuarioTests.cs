using Moq;
using ProyectoConstruccion_APAZA_CUTIPA.Models;
using ProyectoConstruccion_APAZA_CUTIPA.Repositories;
using System;
using System.Collections.Generic;
using Xunit;

namespace TEST_APAZA_CUTIPA
{
    public class AprobacionUsuarioTests
    {
        [Fact]
        public void ObtenerSolicitudesPendientes_ReturnsListOfPendingUsers()
        {
            var mockRepo = new Mock<IUsuarioRepository>();

            var expectedSolicitudes = new List<clsUsuario>
            {
                new clsUsuario { Id = 1, Nombre = "Pending User 1", Correo = "p1@example.com", TipoUsuario = "Invitado", Aprobado = false, FechaRegistro = DateTime.Now.AddDays(-5) },
                new clsUsuario { Id = 2, Nombre = "Pending User 2", Correo = "p2@example.com", TipoUsuario = "Invitado", Aprobado = false, FechaRegistro = DateTime.Now.AddDays(-1) }
            };

            mockRepo.Setup(r => r.ObtenerSolicitudesPendientes())
                    .Returns(expectedSolicitudes);

            var result = mockRepo.Object.ObtenerSolicitudesPendientes();

            Assert.NotNull(result);
            Assert.Equal(expectedSolicitudes.Count, result.Count);

            Assert.Equal(expectedSolicitudes[0].Nombre, result[0].Nombre);
            Assert.Equal(expectedSolicitudes[1].Correo, result[1].Correo);
            Assert.False(result[0].Aprobado);
            Assert.False(result[1].Aprobado);
        }

        [Fact]
        public void ObtenerSolicitudesPendientes_NoPendingUsers_ReturnsEmptyList()
        {
            var mockRepo = new Mock<IUsuarioRepository>();

            mockRepo.Setup(r => r.ObtenerSolicitudesPendientes())
                    .Returns(new List<clsUsuario>());

            var result = mockRepo.Object.ObtenerSolicitudesPendientes();

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void ObtenerSolicitudesPendientes_RepositoryError_ThrowsException()
        {
            var mockRepo = new Mock<IUsuarioRepository>();
            var expectedException = new Exception("Simulated database error during GetPendingRequests");

            mockRepo.Setup(r => r.ObtenerSolicitudesPendientes())
                    .Throws(expectedException);

            var caughtException = Assert.Throws<Exception>(() =>
                 mockRepo.Object.ObtenerSolicitudesPendientes());

            Assert.Equal(expectedException.Message, caughtException.Message);
        }

        [Fact]
        public void AprobarSolicitud_UpdateSucceeds_ReturnsTrue()
        {
            var mockRepo = new Mock<IUsuarioRepository>();
            int usuarioIdAprobar = 10;
            string rolAsignado = "Operador";

            mockRepo.Setup(r => r.AprobarSolicitud(
                                    It.Is<int>(id => id == usuarioIdAprobar),
                                    It.Is<string>(rol => rol == rolAsignado)
                                ))
                    .Returns(true);

            var result = mockRepo.Object.AprobarSolicitud(usuarioIdAprobar, rolAsignado);

            Assert.True(result);
        }

        [Fact]
        public void AprobarSolicitud_UpdateFails_ReturnsFalse()
        {
            var mockRepo = new Mock<IUsuarioRepository>();
            int usuarioIdAprobar = 99;
            string rolAsignado = "Usuario";

            mockRepo.Setup(r => r.AprobarSolicitud(
                                    It.Is<int>(id => id == usuarioIdAprobar),
                                    It.Is<string>(rol => rol == rolAsignado)
                                ))
                    .Returns(false);

            var result = mockRepo.Object.AprobarSolicitud(usuarioIdAprobar, rolAsignado);

            Assert.False(result);
        }

        [Fact]
        public void AprobarSolicitud_RepositoryError_ThrowsException()
        {
            var mockRepo = new Mock<IUsuarioRepository>();
            int usuarioIdAprobar = 10;
            string rolAsignado = "Operador";
            var expectedException = new Exception("Simulated database error during Approval");

            mockRepo.Setup(r => r.AprobarSolicitud(
                                    It.IsAny<int>(),
                                    It.IsAny<string>()
                                ))
                    .Throws(expectedException);

            var caughtException = Assert.Throws<Exception>(() =>
                 mockRepo.Object.AprobarSolicitud(usuarioIdAprobar, rolAsignado));

            Assert.Equal(expectedException.Message, caughtException.Message);
        }
    }
}