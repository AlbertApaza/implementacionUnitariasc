using Xunit;
using Moq;
using System.Web.Mvc;
using ProyectoConstruccion_APAZA_CUTIPA.Controllers;
using ProyectoConstruccion_APAZA_CUTIPA.Models;
using ProyectoConstruccion_APAZA_CUTIPA.Repositories;
using ProyectoConstruccion_APAZA_CUTIPA.Services; 
using System.Web;
using System;
using System.Threading.Tasks;

namespace TEST_APAZA_CUTIPA
{
    public class AutenticarUsuarioTests
    {
        private Mock<IUsuarioRepository> _mockRepo;
        private Mock<IAppConfiguration> _mockConfig;
        private Mock<IGoogleAuthService> _mockGoogleAuthService; 
        private UsuarioController _controller;
        private Mock<HttpSessionStateBase> _mockSession;
        private TempDataDictionary _tempData;

        private void InitializeControllerAndMocks()
        {
            _mockRepo = new Mock<IUsuarioRepository>();
            _mockConfig = new Mock<IAppConfiguration>();
            _mockGoogleAuthService = new Mock<IGoogleAuthService>(); 
            _mockSession = new Mock<HttpSessionStateBase>();
            _tempData = new TempDataDictionary();

            var mockHttpContext = new Mock<HttpContextBase>();
            mockHttpContext.Setup(ctx => ctx.Session).Returns(_mockSession.Object);

            var mockControllerContext = new Mock<ControllerContext>();
            mockControllerContext.Setup(cc => cc.HttpContext).Returns(mockHttpContext.Object);

            _controller = new UsuarioController(_mockRepo.Object, _mockConfig.Object, _mockGoogleAuthService.Object)
            {
                ControllerContext = mockControllerContext.Object,
                TempData = _tempData
            };

            _mockConfig.Setup(c => c.FlutterAppAuthCallbackScheme).Returns("testappscheme");
            _mockConfig.Setup(c => c.JwtSecretKey).Returns("ESTA_ES_UNA_CLAVE_SECRETA_MUY_LARGA_PARA_TESTS_DE_JWT" + Guid.NewGuid().ToString());
            _mockConfig.Setup(c => c.JwtIssuer).Returns("testissuer.com");
            _mockConfig.Setup(c => c.JwtAudience).Returns("testaudience.com");
            _mockConfig.Setup(c => c.GoogleClientId).Returns("test-google-client-id");
            _mockConfig.Setup(c => c.GoogleClientSecret).Returns("test-google-client-secret");
            _mockConfig.Setup(c => c.GoogleRedirectUri).Returns("http://localhost/test/oauth2callback");
        }

        [Fact]
        public void Login_Credenciales_UsuarioValidoYAprobado_AccesoExitoso()
        {
            InitializeControllerAndMocks();
            string correo = "usuario.valido@example.com";
            string contrasena = "Password123";
            string origin = "webapp";
            var usuarioEsperado = new clsUsuario { Id = 1, Nombre = "Usuario Valido", Correo = correo, Aprobado = true, TipoUsuario = "Usuario" };
            string msgErrorSalida = "";
            bool esAprobadoSalida = true;

            _mockRepo.Setup(r => r.Autenticar(correo, contrasena, out msgErrorSalida, out esAprobadoSalida))
                    .Returns(usuarioEsperado);

            var result = _controller.Login(correo, contrasena, origin) as RedirectToRouteResult;

            Assert.NotNull(result);
            Assert.Equal("Index", result.RouteValues["action"]);
            Assert.Equal("Home", result.RouteValues["controller"]);
            _mockSession.VerifySet(s => s["Usuario"] = usuarioEsperado, Times.Once);
        }

        [Fact]
        public void Registro_Credenciales_UsuarioNuevo_CreacionPendienteRedireccionALogin()
        {
            InitializeControllerAndMocks();
            var usuarioForm = new clsUsuario { Nombre = "Nuevo Usuario", Correo = "nuevo.usuario@example.com", Contrasena = "P@sswordNuevo" };
            string confirmarContrasena = "P@sswordNuevo";

            bool correoExisteAprobadoSalida = false;
            string msgExistenciaSalida = "";
            _mockRepo.Setup(r => r.VerificarCorreoExistente(usuarioForm.Correo, out correoExisteAprobadoSalida, out msgExistenciaSalida))
                    .Returns(false);

            string msgErrorRegistroSalida = "";
            _mockRepo.Setup(r => r.Registrar(It.Is<clsUsuario>(u => u.Correo == usuarioForm.Correo), out msgErrorRegistroSalida))
                    .Returns(true);

            var result = _controller.Registro(usuarioForm, confirmarContrasena) as RedirectToRouteResult;

            Assert.NotNull(result);
            Assert.Equal("Login", result.RouteValues["action"]);
            Assert.Equal("Registro exitoso. Por favor, inicie sesión o espere aprobación.", _tempData["RegistroExitoso"]);
        }

        [Fact]
        public async Task Login_Google_UsuarioNuevo_CreacionPendienteMensajeDeEspera() 
        {
            InitializeControllerAndMocks();
            string authCodeFromGoogle = "google_auth_code_nuevo_usuario";
            string origin = "webapp";
            _tempData["Origin"] = origin;

            string emailDesdeGoogle = "nuevo.google@example.com";
            string nombreDesdeGoogle = "Nuevo Google User";
            var googleUserInfoMock = new GoogleUserInfo { Email = emailDesdeGoogle, Name = nombreDesdeGoogle, Error = null };

            _mockGoogleAuthService.Setup(s => s.GetUserInfoFromGoogleCodeAsync(authCodeFromGoogle,
                                                                                _mockConfig.Object.GoogleClientId,
                                                                                _mockConfig.Object.GoogleClientSecret,
                                                                                _mockConfig.Object.GoogleRedirectUri))
                                  .ReturnsAsync(googleUserInfoMock);

            bool necesitaAprobacionSalida = true;
            string mensajeProcesoSalida = "Petición de Acceso con Google enviada. Pendiente de aprobación.";
            var usuarioDevueltoPorRepo = new clsUsuario { Correo = emailDesdeGoogle, Nombre = nombreDesdeGoogle, Aprobado = false };

            _mockRepo.Setup(r => r.ObtenerOInsertarUsuarioGoogle(emailDesdeGoogle, nombreDesdeGoogle, out necesitaAprobacionSalida, out mensajeProcesoSalida))
                    .Returns(usuarioDevueltoPorRepo)
                    .Callback(new ObtenerOInsertarUsuarioGoogleDelegate((string e, string n, out bool na, out string mp) =>
                    {
                        na = true;
                        mp = "Petición de Acceso con Google enviada. Pendiente de aprobación.";
                    }));

            var actionResult = await _controller.GoogleCallback(authCodeFromGoogle);
            var result = Assert.IsType<ViewResult>(actionResult);

            Assert.Equal("Mensaje", result.ViewName);
            Assert.Equal(mensajeProcesoSalida, result.ViewBag.Mensaje);
        }

        private delegate void ObtenerOInsertarUsuarioGoogleDelegate(string email, string name, out bool necesitaAprobacion, out string mensajeProceso);


        [Fact]
        public async Task Login_Google_UsuarioExistenteYAprobado_AccesoExitoso() 
        {
            InitializeControllerAndMocks();
            string authCodeFromGoogle = "google_auth_code_usuario_existente";
            string origin = "webapp";
            _tempData["Origin"] = origin;

            string emailDesdeGoogle = "existente.aprobado@example.com";
            string nombreDesdeGoogle = "Existente Aprobado Google";
            var googleUserInfoMock = new GoogleUserInfo { Email = emailDesdeGoogle, Name = nombreDesdeGoogle, Error = null };

            _mockGoogleAuthService.Setup(s => s.GetUserInfoFromGoogleCodeAsync(authCodeFromGoogle,
                                                                                _mockConfig.Object.GoogleClientId,
                                                                                _mockConfig.Object.GoogleClientSecret,
                                                                                _mockConfig.Object.GoogleRedirectUri))
                                  .ReturnsAsync(googleUserInfoMock);


            bool necesitaAprobacionSalida = false;
            string mensajeProcesoSalida = "Login con Google exitoso.";
            var usuarioEsperado = new clsUsuario { Id = 3, Correo = emailDesdeGoogle, Nombre = nombreDesdeGoogle, Aprobado = true, TipoUsuario = "UsuarioGoogle" };

            _mockRepo.Setup(r => r.ObtenerOInsertarUsuarioGoogle(emailDesdeGoogle, nombreDesdeGoogle, out necesitaAprobacionSalida, out mensajeProcesoSalida))
                    .Returns(usuarioEsperado)
                    .Callback(new ObtenerOInsertarUsuarioGoogleDelegate((string e, string n, out bool na, out string mp) =>
                    {
                        na = false;
                        mp = "Login con Google exitoso.";
                    }));

            var actionResult = await _controller.GoogleCallback(authCodeFromGoogle);
            var result = Assert.IsType<RedirectToRouteResult>(actionResult);

            Assert.Equal("Index", result.RouteValues["action"]);
            Assert.Equal("Home", result.RouteValues["controller"]);
            _mockSession.VerifySet(s => s["Usuario"] = usuarioEsperado, Times.Once);
        }

        [Fact]
        public async Task Login_Google_ServicioGoogleFalla_RetornaLoginViewConError() 
        {
            InitializeControllerAndMocks();
            string authCodeFromGoogle = "google_auth_code_falla_servicio";
            string origin = "webapp";
            _tempData["Origin"] = origin;

            var googleUserInfoMock = new GoogleUserInfo { Error = "Simulated Google Service Error" };

            _mockGoogleAuthService.Setup(s => s.GetUserInfoFromGoogleCodeAsync(authCodeFromGoogle,
                                                                                _mockConfig.Object.GoogleClientId,
                                                                                _mockConfig.Object.GoogleClientSecret,
                                                                                _mockConfig.Object.GoogleRedirectUri))
                                  .ReturnsAsync(googleUserInfoMock);

            var actionResult = await _controller.GoogleCallback(authCodeFromGoogle);
            var result = Assert.IsType<ViewResult>(actionResult);

            Assert.Equal("Login", result.ViewName);
            Assert.Contains("Simulated Google Service Error", result.ViewBag.Mensaje as string);
            _mockRepo.Verify(r => r.ObtenerOInsertarUsuarioGoogle(It.IsAny<string>(), It.IsAny<string>(), out It.Ref<bool>.IsAny, out It.Ref<string>.IsAny), Times.Never);
        }


        [Fact]
        public void Login_Credenciales_CredencialesIncorrectas_ErrorPermaneceEnLogin()
        {
            InitializeControllerAndMocks();
            string correo = "usuario.existente@example.com";
            string contrasenaIncorrecta = "PasswordErronea";
            string origin = "webapp";
            string mensajeErrorEsperado = "Contraseña incorrecta.";
            string msgErrorSalida = mensajeErrorEsperado;
            bool esAprobadoSalida = false;

            _mockRepo.Setup(r => r.Autenticar(correo, contrasenaIncorrecta, out msgErrorSalida, out esAprobadoSalida))
                    .Returns((clsUsuario)null);

            var result = _controller.Login(correo, contrasenaIncorrecta, origin) as ViewResult;

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.ViewName);
            Assert.Equal(mensajeErrorEsperado, result.ViewBag.Mensaje);
            Assert.Equal(origin, result.ViewBag.Origin);
            _mockSession.VerifySet(s => s["Usuario"] = It.IsAny<clsUsuario>(), Times.Never);
        }
    }
}