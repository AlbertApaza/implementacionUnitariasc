using Moq;
using ProyectoConstruccion_APAZA_CUTIPA.Models;
using ProyectoConstruccion_APAZA_CUTIPA.Repositories;
using System;
using Xunit;

namespace TEST_APAZA_CUTIPA
{
    public class RecuperarContrasena
    {
        [Fact]
        public void ExistePorCorreo_EmailExists_ReturnsTrue()
        {
            var mockRepo = new Mock<IUsuarioRepository>();
            string emailAProbar = "correoexistente@example.com";

            mockRepo.Setup(r => r.ExistePorCorreo(emailAProbar))
                    .Returns(true);

            var result = mockRepo.Object.ExistePorCorreo(emailAProbar);

            Assert.True(result);
        }

        [Fact]
        public void ExistePorCorreo_EmailDoesNotExist_ReturnsFalse()
        {
            var mockRepo = new Mock<IUsuarioRepository>();
            string emailAProbar = "correonoexistente@example.com";

            mockRepo.Setup(r => r.ExistePorCorreo(emailAProbar))
                    .Returns(false);

            var result = mockRepo.Object.ExistePorCorreo(emailAProbar);

            Assert.False(result);
        }

        [Fact]
        public void ExistePorCorreo_RepositoryError_ThrowsException()
        {
            var mockRepo = new Mock<IUsuarioRepository>();
            string emailAProbar = "correoerror@example.com";
            var expectedException = new Exception("Simulated database error during ExistCheck");

            mockRepo.Setup(r => r.ExistePorCorreo(emailAProbar))
                    .Throws(expectedException);

            var caughtException = Assert.Throws<Exception>(() =>
                 mockRepo.Object.ExistePorCorreo(emailAProbar));

            Assert.Equal(expectedException.Message, caughtException.Message);
        }

        [Fact]
        public void ActualizarContrasena_UpdateSucceeds_ReturnsTrue()
        {
            var mockRepo = new Mock<IUsuarioRepository>();
            string emailUsuario = "usuario@example.com";
            string nuevaContrasena = "NuevaClaveSegura123";

            mockRepo.Setup(r => r.ActualizarContrasena(emailUsuario, nuevaContrasena))
                    .Returns(true);

            var result = mockRepo.Object.ActualizarContrasena(emailUsuario, nuevaContrasena);

            Assert.True(result);
        }

        [Fact]
        public void ActualizarContrasena_UserDoesNotExistOrUpdateFails_ReturnsFalse()
        {
            var mockRepo = new Mock<IUsuarioRepository>();
            string emailUsuario = "usuarioinexistente@example.com";
            string nuevaContrasena = "CualquierClave";

            mockRepo.Setup(r => r.ActualizarContrasena(emailUsuario, nuevaContrasena))
                    .Returns(false);

            var result = mockRepo.Object.ActualizarContrasena(emailUsuario, nuevaContrasena);

            Assert.False(result);
        }

        [Fact]
        public void ActualizarContrasena_RepositoryError_ThrowsException()
        {
            var mockRepo = new Mock<IUsuarioRepository>();
            string emailUsuario = "usuarioerror@example.com";
            string nuevaContrasena = "ClaveConError";
            var expectedException = new Exception("Simulated database connection error during UpdatePassword");

            mockRepo.Setup(r => r.ActualizarContrasena(It.IsAny<string>(), It.IsAny<string>()))
                   .Throws(expectedException);

            var caughtException = Assert.Throws<Exception>(() =>
                 mockRepo.Object.ActualizarContrasena(emailUsuario, nuevaContrasena));

            Assert.Equal(expectedException.Message, caughtException.Message);
        }
    }
}