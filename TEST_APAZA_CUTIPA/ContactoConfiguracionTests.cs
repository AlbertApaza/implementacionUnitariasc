using Moq;
using ProyectoConstruccion_APAZA_CUTIPA.Models;
using ProyectoConstruccion_APAZA_CUTIPA.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace TEST_APAZA_CUTIPA
{
    public class ContactoConfiguracionTests
    {
        private List<clsContactoEmergencia> CreateSampleContacts(int userId)
        {
            return new List<clsContactoEmergencia>
            {
                new clsContactoEmergencia { IdContactoEmergencia = 1, IdUsuario = userId, Nombre = "Luis", Apellido = "Paredes", NumeroTelefono = "987654321", Parentesco = "Padre", EsPrincipal = true, FechaCreacion = DateTime.Now.AddDays(-10) },
                new clsContactoEmergencia { IdContactoEmergencia = 2, IdUsuario = userId, Nombre = "Ana", Apellido = "Gomez", NumeroTelefono = "123456789", Parentesco = "Madre", EsPrincipal = false, FechaCreacion = DateTime.Now.AddDays(-5) },
                 new clsContactoEmergencia { IdContactoEmergencia = 3, IdUsuario = userId, Nombre = "Carlos", Apellido = "Lopez", NumeroTelefono = "555555555", Parentesco = "Hermano", EsPrincipal = false, FechaCreacion = DateTime.Now.AddDays(-3) },
            };
        }

        [Fact]
        public void GetByUserId_UserHasContacts_ReturnsListOfContacts()
        {
            var mockRepo = new Mock<IContactoRepository>();
            int userId = 123;
            var expectedContacts = CreateSampleContacts(userId);
            string expectedError = string.Empty;

            mockRepo.Setup(r => r.GetByUserId(userId, out expectedError))
                    .Returns(expectedContacts);

            string errorOut;
            var result = mockRepo.Object.GetByUserId(userId, out errorOut);

            Assert.NotNull(result);
            Assert.Equal(expectedContacts.Count, result.Count);
            Assert.Equal(expectedContacts[0].Nombre, result[0].Nombre);
            Assert.Equal(expectedError, errorOut);
        }

        [Fact]
        public void GetByUserId_UserHasNoContacts_ReturnsEmptyList()
        {
            var mockRepo = new Mock<IContactoRepository>();
            int userId = 456;
            var expectedContacts = new List<clsContactoEmergencia>();
            string expectedError = string.Empty;

            mockRepo.Setup(r => r.GetByUserId(userId, out expectedError))
                    .Returns(expectedContacts);

            string errorOut;
            var result = mockRepo.Object.GetByUserId(userId, out errorOut);

            Assert.NotNull(result);
            Assert.Empty(result);
            Assert.Equal(expectedError, errorOut);
        }

        [Fact]
        public void GetByUserId_RepositoryError_ReturnsEmptyListAndError()
        {
            var mockRepo = new Mock<IContactoRepository>();
            int userId = 789;
            var expectedContacts = new List<clsContactoEmergencia>();
            string expectedError = "DB Connection Error";

            mockRepo.Setup(r => r.GetByUserId(userId, out expectedError))
                    .Returns(expectedContacts);

            string errorOut;
            var result = mockRepo.Object.GetByUserId(userId, out errorOut);

            Assert.NotNull(result);
            Assert.Empty(result);
            Assert.Equal(expectedError, errorOut);
        }

        [Fact]
        public void Add_ValidContact_ReturnsTrueAndEmptyError()
        {
            var mockRepo = new Mock<IContactoRepository>();
            var newContact = new clsContactoEmergencia { IdUsuario = 123, Nombre = "Nuevo", NumeroTelefono = "111" };
            string expectedError = string.Empty;

            mockRepo.Setup(r => r.Add(It.Is<clsContactoEmergencia>(c => c.IdUsuario == 123), out expectedError))
                    .Returns(true);

            string errorOut;
            var result = mockRepo.Object.Add(newContact, out errorOut);

            Assert.True(result);
            Assert.Equal(expectedError, errorOut);
        }

        [Fact]
        public void Add_RepositoryError_ReturnsFalseAndError()
        {
            var mockRepo = new Mock<IContactoRepository>();
            var newContact = new clsContactoEmergencia { IdUsuario = 123, Nombre = "Nuevo", NumeroTelefono = "111" };
            string expectedError = "DB Insert Failed";

            mockRepo.Setup(r => r.Add(It.Is<clsContactoEmergencia>(c => c.IdUsuario == 123), out expectedError))
                    .Returns(false);

            string errorOut;
            var result = mockRepo.Object.Add(newContact, out errorOut);

            Assert.False(result);
            Assert.Equal(expectedError, errorOut);
        }

        [Fact]
        public void GetById_ContactExistsForUser_ReturnsContact()
        {
            var mockRepo = new Mock<IContactoRepository>();
            int contactId = 1;
            int userId = 123;
            var expectedContact = CreateSampleContacts(userId).First(c => c.IdContactoEmergencia == contactId);
            string expectedError = string.Empty;

            mockRepo.Setup(r => r.GetById(contactId, userId, out expectedError))
                    .Returns(expectedContact);

            string errorOut;
            var result = mockRepo.Object.GetById(contactId, userId, out errorOut);

            Assert.NotNull(result);
            Assert.Equal(expectedContact.IdContactoEmergencia, result.IdContactoEmergencia);
            Assert.Equal(expectedError, errorOut);
        }

        [Fact]
        public void GetById_ContactDoesNotExistForUser_ReturnsNullAndError()
        {
            var mockRepo = new Mock<IContactoRepository>();
            int contactId = 999;
            int userId = 123;
            string expectedError = "Contacto no encontrado para este usuario.";

            mockRepo.Setup(r => r.GetById(contactId, userId, out expectedError))
                    .Returns((clsContactoEmergencia)null);

            string errorOut;
            var result = mockRepo.Object.GetById(contactId, userId, out errorOut);

            Assert.Null(result);
            Assert.Equal(expectedError, errorOut);
        }

        [Fact]
        public void GetById_RepositoryError_ReturnsNullAndError()
        {
            var mockRepo = new Mock<IContactoRepository>();
            int contactId = 1;
            int userId = 123;
            string expectedError = "DB Read Error";

            mockRepo.Setup(r => r.GetById(contactId, userId, out expectedError))
                    .Returns((clsContactoEmergencia)null);

            string errorOut;
            var result = mockRepo.Object.GetById(contactId, userId, out errorOut);

            Assert.Null(result);
            Assert.Equal(expectedError, errorOut);
        }

        [Fact]
        public void Update_ValidContact_ReturnsTrueAndEmptyError()
        {
            var mockRepo = new Mock<IContactoRepository>();
            var updatedContact = new clsContactoEmergencia { IdContactoEmergencia = 1, IdUsuario = 123, Nombre = "Luis (Updated)", NumeroTelefono = "999", EsPrincipal = false };
            string expectedError = string.Empty;

            mockRepo.Setup(r => r.Update(It.Is<clsContactoEmergencia>(c => c.IdContactoEmergencia == 1 && c.IdUsuario == 123), out expectedError))
                    .Returns(true);

            string errorOut;
            var result = mockRepo.Object.Update(updatedContact, out errorOut);

            Assert.True(result);
            Assert.Equal(expectedError, errorOut);
        }

        [Fact]
        public void Update_ContactDoesNotExistForUser_ReturnsFalseAndError()
        {
            var mockRepo = new Mock<IContactoRepository>();
            var updatedContact = new clsContactoEmergencia { IdContactoEmergencia = 999, IdUsuario = 123, Nombre = "Luis (Updated)", NumeroTelefono = "999", EsPrincipal = false };
            string expectedError = "Contacto no encontrado para este usuario para actualizar.";

            mockRepo.Setup(r => r.Update(It.Is<clsContactoEmergencia>(c => c.IdContactoEmergencia == 999 && c.IdUsuario == 123), out expectedError))
                    .Returns(false);

            string errorOut;
            var result = mockRepo.Object.Update(updatedContact, out errorOut);

            Assert.False(result);
            Assert.Equal(expectedError, errorOut);
        }


        [Fact]
        public void Update_RepositoryError_ReturnsFalseAndError()
        {
            var mockRepo = new Mock<IContactoRepository>();
            var updatedContact = new clsContactoEmergencia { IdContactoEmergencia = 1, IdUsuario = 123, Nombre = "Luis (Updated)", NumeroTelefono = "999", EsPrincipal = false };
            string expectedError = "DB Update Failed";

            mockRepo.Setup(r => r.Update(It.Is<clsContactoEmergencia>(c => c.IdContactoEmergencia == 1 && c.IdUsuario == 123), out expectedError))
                    .Returns(false);

            string errorOut;
            var result = mockRepo.Object.Update(updatedContact, out errorOut);

            Assert.False(result);
            Assert.Equal(expectedError, errorOut);
        }

        [Fact]
        public void SetAsPrincipal_ValidId_ReturnsTrueAndEmptyError()
        {
            var mockRepo = new Mock<IContactoRepository>();
            int contactIdToMark = 2;
            int userId = 123;
            string expectedError = string.Empty;

            mockRepo.Setup(r => r.SetAsPrincipal(contactIdToMark, userId, out expectedError))
                    .Returns(true);

            string errorOut;
            var result = mockRepo.Object.SetAsPrincipal(contactIdToMark, userId, out errorOut);

            Assert.True(result);
            Assert.Equal(expectedError, errorOut);
        }

        [Fact]
        public void SetAsPrincipal_InvalidIdOrContactDoesNotBelongToUser_ReturnsFalseAndError()
        {
            var mockRepo = new Mock<IContactoRepository>();
            int contactIdToMark = 999;
            int userId = 123;
            string expectedError = "Contacto no encontrado o no pertenece al usuario.";

            mockRepo.Setup(r => r.SetAsPrincipal(contactIdToMark, userId, out expectedError))
                    .Returns(false);

            string errorOut;
            var result = mockRepo.Object.SetAsPrincipal(contactIdToMark, userId, out errorOut);

            Assert.False(result);
            Assert.Equal(expectedError, errorOut);
        }


        [Fact]
        public void SetAsPrincipal_RepositoryError_ReturnsFalseAndError()
        {
            var mockRepo = new Mock<IContactoRepository>();
            int contactIdToMark = 2;
            int userId = 123;
            string expectedError = "DB Transaction Failed";

            mockRepo.Setup(r => r.SetAsPrincipal(contactIdToMark, userId, out expectedError))
                    .Returns(false);

            string errorOut;
            var result = mockRepo.Object.SetAsPrincipal(contactIdToMark, userId, out errorOut);

            Assert.False(result);
            Assert.Equal(expectedError, errorOut);
        }

        [Fact]
        public void Delete_ValidId_ReturnsTrueAndEmptyError()
        {
            var mockRepo = new Mock<IContactoRepository>();
            int contactIdToDelete = 2;
            int userId = 123;
            string expectedError = string.Empty;

            mockRepo.Setup(r => r.Delete(contactIdToDelete, userId, out expectedError))
                    .Returns(true);

            string errorOut;
            var result = mockRepo.Object.Delete(contactIdToDelete, userId, out errorOut);

            Assert.True(result);
            Assert.Equal(expectedError, errorOut);
        }

        [Fact]
        public void Delete_InvalidIdOrContactDoesNotExistForUser_ReturnsFalseAndError()
        {
            var mockRepo = new Mock<IContactoRepository>();
            int contactIdToDelete = 999;
            int userId = 123;
            string expectedError = "Contacto no encontrado para eliminar.";

            mockRepo.Setup(r => r.Delete(contactIdToDelete, userId, out expectedError))
                    .Returns(false);

            string errorOut;
            var result = mockRepo.Object.Delete(contactIdToDelete, userId, out errorOut);

            Assert.False(result);
            Assert.Equal(expectedError, errorOut);
        }


        [Fact]
        public void Delete_RepositoryError_ReturnsFalseAndError()
        {
            var mockRepo = new Mock<IContactoRepository>();
            int contactIdToDelete = 2;
            int userId = 123;
            string expectedError = "DB Delete Failed";

            mockRepo.Setup(r => r.Delete(contactIdToDelete, userId, out expectedError))
                    .Returns(false);

            string errorOut;
            var result = mockRepo.Object.Delete(contactIdToDelete, userId, out errorOut);

            Assert.False(result);
            Assert.Equal(expectedError, errorOut);
        }
    }
}