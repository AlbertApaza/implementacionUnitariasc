using System;
using System.Configuration;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;
using ProyectoConstruccion_APAZA_CUTIPA.Models;
using ProyectoConstruccion_APAZA_CUTIPA.Repositories;
using ProyectoConstruccion_APAZA_CUTIPA.Services; // Asegúrate que este using apunte a donde creaste IGoogleAuthService
using Google.Apis.Auth;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Net;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Net.Http;

public interface IAppConfiguration
{
    string GoogleClientId { get; }
    string GoogleClientSecret { get; }
    string GoogleRedirectUri { get; }
    string JwtSecretKey { get; }
    string JwtIssuer { get; }
    string JwtAudience { get; }
    string GoogleClientIdForApiValidation { get; }
    string FlutterAppAuthCallbackScheme { get; }
}

public class AppConfiguration : IAppConfiguration
{
    public string GoogleClientId => ConfigurationManager.AppSettings["GoogleClientId"];
    public string GoogleClientSecret => ConfigurationManager.AppSettings["GoogleClientSecret"];
    public string GoogleRedirectUri => ConfigurationManager.AppSettings["GoogleRedirectUri"];
    public string JwtSecretKey => ConfigurationManager.AppSettings["JwtSecretKey"];
    public string JwtIssuer => ConfigurationManager.AppSettings["JwtIssuer"];
    public string JwtAudience => ConfigurationManager.AppSettings["JwtAudience"];
    public string GoogleClientIdForApiValidation => ConfigurationManager.AppSettings["GoogleClientIdForApiValidation"];
    public string FlutterAppAuthCallbackScheme => ConfigurationManager.AppSettings["FlutterAppAuthCallbackScheme"];
}

namespace ProyectoConstruccion_APAZA_CUTIPA.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly IUsuarioRepository _repository;
        private readonly IAppConfiguration _config;
        private readonly IGoogleAuthService _googleAuthService; // Nueva dependencia

        public UsuarioController(IUsuarioRepository repository, IAppConfiguration config, IGoogleAuthService googleAuthService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _googleAuthService = googleAuthService ?? throw new ArgumentNullException(nameof(googleAuthService)); // Inicializar
        }

        public UsuarioController()
            : this(new UsuarioRepositoryMySQL(), new AppConfiguration(), new GoogleAuthService()) // Añadir la implementación por defecto
        {
        }

        public ActionResult Login(string origin)
        {
            ViewBag.Origin = origin;
            return View();
        }

        [HttpPost]
        public ActionResult Login(string correo, string contrasena, string origin)
        {
            string mensajeError;
            bool esAprobado;
            clsUsuario usuario = _repository.Autenticar(correo, contrasena, out mensajeError, out esAprobado);

            if (usuario != null)
            {
                if ("flutterapp".Equals(origin, StringComparison.OrdinalIgnoreCase))
                {
                    string jwt = GenerateJwtToken(usuario.Id, usuario.Correo, usuario.Nombre, usuario.TipoUsuario);
                    string callbackUrl = $"{_config.FlutterAppAuthCallbackScheme}://auth/callback?token={Uri.EscapeDataString(jwt)}";
                    return Redirect(callbackUrl);
                }
                else
                {
                    Session["Usuario"] = usuario;
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                ViewBag.Mensaje = mensajeError;
                ViewBag.Origin = origin;
                return View();
            }
        }

        public ActionResult Registro()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Registro(clsUsuario usuarioForm, string confirmarContrasena)
        {
            if (usuarioForm.Contrasena != confirmarContrasena)
            {
                ViewBag.Mensaje = "Las contraseñas no coinciden.";
                return View(usuarioForm);
            }

            bool correoExistenteEstaAprobado;
            string mensajeExistencia;
            if (_repository.VerificarCorreoExistente(usuarioForm.Correo, out correoExistenteEstaAprobado, out mensajeExistencia))
            {
                ViewBag.Mensaje = mensajeExistencia;
                return View(usuarioForm);
            }

            string mensajeErrorRegistro;
            if (_repository.Registrar(usuarioForm, out mensajeErrorRegistro))
            {
                TempData["RegistroExitoso"] = "Registro exitoso. Por favor, inicie sesión o espere aprobación.";
                return RedirectToAction("Login");
            }
            else
            {
                ViewBag.Mensaje = mensajeErrorRegistro;
                return View(usuarioForm);
            }
        }

        public ActionResult GoogleLogin(string origin)
        {
            TempData["Origin"] = origin;
            string url = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                         $"scope=email%20profile&" +
                         $"response_type=code&" +
                         $"redirect_uri={Uri.EscapeDataString(_config.GoogleRedirectUri)}&" +
                         $"client_id={_config.GoogleClientId}&" +
                         $"access_type=online";
            return Redirect(url);
        }

        public async Task<ActionResult> GoogleCallback(string code) // Cambiado a async Task<ActionResult>
        {
            string origin = TempData["Origin"] as string;
            if (string.IsNullOrEmpty(code))
            {
                ViewBag.Mensaje = "Error al autenticarse con Google: código no proporcionado.";
                ViewBag.Origin = origin;
                return View("Login");
            }

            GoogleUserInfo googleUser = await _googleAuthService.GetUserInfoFromGoogleCodeAsync(
                code,
                _config.GoogleClientId,
                _config.GoogleClientSecret,
                _config.GoogleRedirectUri
            );

            if (googleUser == null || !string.IsNullOrEmpty(googleUser.Error) || string.IsNullOrEmpty(googleUser.Email) || string.IsNullOrEmpty(googleUser.Name))
            {
                ViewBag.Mensaje = "Error obteniendo información de Google: " + (googleUser?.Error ?? "Información de usuario incompleta.");
                ViewBag.Origin = origin;
                return View("Login");
            }

            string userEmail = googleUser.Email;
            string userName = googleUser.Name;

            bool necesitaAprobacion;
            string mensajeProceso;
            clsUsuario usuario = _repository.ObtenerOInsertarUsuarioGoogle(userEmail, userName, out necesitaAprobacion, out mensajeProceso);

            if (necesitaAprobacion || (usuario != null && !usuario.Aprobado))
            {
                ViewBag.Mensaje = mensajeProceso;
                return View("Mensaje");
            }

            if (usuario != null && usuario.Aprobado)
            {
                if ("flutterapp".Equals(origin, StringComparison.OrdinalIgnoreCase))
                {
                    string jwt = GenerateJwtToken(usuario.Id, usuario.Correo, usuario.Nombre, usuario.TipoUsuario);
                    string callbackUrl = $"{_config.FlutterAppAuthCallbackScheme}://auth/callback?token={Uri.EscapeDataString(jwt)}";
                    return Redirect(callbackUrl);
                }
                else
                {
                    Session["Usuario"] = usuario;
                    return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.Mensaje = "Error inesperado durante el proceso de Google Login o usuario no aprobado.";
            ViewBag.Origin = origin;
            return View("Login");
        }

        public class GoogleSignInApiPayload { public string IdToken { get; set; } }

        [HttpPost]
        public async Task<JsonResult> GoogleSignInApi(GoogleSignInApiPayload payloadData)
        {
            if (payloadData == null || string.IsNullOrEmpty(payloadData.IdToken))
            {
                return Json(new { Success = false, Message = "ID Token de Google es requerido." });
            }
            try
            {
                var validationSettings = new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { _config.GoogleClientIdForApiValidation } };
                GoogleJsonWebSignature.Payload googlePayload = await GoogleJsonWebSignature.ValidateAsync(payloadData.IdToken, validationSettings);
                string email = googlePayload.Email;
                string name = googlePayload.Name;

                bool esNuevoUsuarioPendiente;
                bool yaAprobado;
                string mensajeModelo;
                clsUsuario usuario = _repository.ProcesarUsuarioGoogleApi(email, name, out esNuevoUsuarioPendiente, out yaAprobado, out mensajeModelo);

                if (esNuevoUsuarioPendiente && !yaAprobado)
                {
                    return Json(new { Success = true, Message = mensajeModelo, IsPendingApproval = true });
                }
                else if (yaAprobado && usuario != null)
                {
                    var userForToken = new { Id = usuario.Id, Nombre = usuario.Nombre, Correo = usuario.Correo, TipoUsuario = usuario.TipoUsuario };
                    string token = GenerateJwtToken(userForToken.Id, userForToken.Correo, userForToken.Nombre, userForToken.TipoUsuario);
                    return Json(new { Success = true, Token = token, User = userForToken, Message = mensajeModelo });
                }
                else
                {
                    return Json(new { Success = false, Message = mensajeModelo ?? "Error desconocido durante el proceso API de Google." });
                }
            }
            catch (InvalidJwtException ex)
            {
                return Json(new { Success = false, Message = "Token de Google inválido: " + ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Message = "Error interno API: " + ex.Message });
            }
        }

        private string GenerateJwtToken(int userId, string userEmail, string userName, string userRole)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.JwtSecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, userEmail),
                new Claim("nombre", userName),
                new Claim(ClaimTypes.Role, userRole),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            var token = new JwtSecurityToken(
                issuer: _config.JwtIssuer,
                audience: _config.JwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ActionResult Solicitudes()
        {
            var usuarioSesion = Session["Usuario"] as clsUsuario;
            if (usuarioSesion == null || usuarioSesion.TipoUsuario != "Administrador")
            {
                return RedirectToAction("Login");
            }
            List<clsUsuario> solicitudes = _repository.ObtenerSolicitudesPendientes();
            return View(solicitudes);
        }

        [HttpPost]
        public ActionResult AprobarSolicitud(int id, string rolAsignado)
        {
            var usuarioSesion = Session["Usuario"] as clsUsuario;
            if (usuarioSesion == null || usuarioSesion.TipoUsuario != "Administrador")
            {
                return RedirectToAction("Login");
            }

            if (string.IsNullOrEmpty(rolAsignado))
            {
                TempData["ErrorSolicitud"] = "Debe seleccionar un rol para aprobar.";
                return RedirectToAction("Solicitudes");
            }

            if (_repository.AprobarSolicitud(id, rolAsignado))
            {
                TempData["ExitoSolicitud"] = "Usuario aprobado correctamente.";
            }
            else
            {
                TempData["ErrorSolicitud"] = "No se pudo aprobar la solicitud.";
            }
            return RedirectToAction("Solicitudes");
        }

        [HttpGet]
        public ActionResult Recuperar()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Recuperar(string correo)
        {
            if (_repository.ExistePorCorreo(correo))
            {
                string codigo = new Random().Next(100000, 999999).ToString();
                Session["CodigoRecuperacion"] = codigo;
                Session["CorreoRecuperacion"] = correo;
                try
                {
                    var mensaje = new System.Net.Mail.MailMessage();
                    mensaje.To.Add(correo);
                    mensaje.Subject = "Recuperación de contraseña";
                    mensaje.Body = $"Tu código de recuperación es: {codigo}";
                    var smtp = new System.Net.Mail.SmtpClient();
                    smtp.Send(mensaje);
                    return RedirectToAction("VerificarCodigo");
                }
                catch (Exception ex)
                {
                    ViewBag.Mensaje = "Error al enviar el correo: " + ex.Message;
                    return View();
                }
            }
            else
            {
                ViewBag.Mensaje = "Correo no registrado.";
                return View();
            }
        }

        [HttpGet]
        public ActionResult VerificarCodigo()
        {
            if (Session["CorreoRecuperacion"] == null) return RedirectToAction("Login");
            return View();
        }

        [HttpPost]
        public ActionResult VerificarCodigo(string codigo)
        {
            if (Session["CodigoRecuperacion"]?.ToString() == codigo)
            {
                return RedirectToAction("CambiarPassword");
            }
            ViewBag.Mensaje = "Código incorrecto.";
            return View();
        }

        [HttpGet]
        public ActionResult CambiarPassword()
        {
            if (Session["CorreoRecuperacion"] == null || Session["CodigoRecuperacion"] == null) return RedirectToAction("Login");
            return View();
        }

        [HttpPost]
        public ActionResult CambiarPassword(string pass1, string pass2)
        {
            if (pass1 != pass2)
            {
                ViewBag.Mensaje = "Las contraseñas no coinciden.";
                return View();
            }
            string correo = Session["CorreoRecuperacion"]?.ToString();
            if (string.IsNullOrEmpty(correo))
            {
                return RedirectToAction("Login");
            }

            ViewBag.Mensaje = "Funcionalidad de cambio de contraseña no conectada al repositorio actual.";
            return View();
        }

        [HttpGet]
        public async Task<ActionResult> PruebaNotificacion()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    var alertData = new { objeto = "prueba", confianza = 1.0, fuente = "Sistema de Prueba MVC" };
                    var json = JsonConvert.SerializeObject(alertData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync("http://localhost:5001/alert", content);
                    return Json(new { success = response.IsSuccessStatusCode, message = response.IsSuccessStatusCode ? "Notificación enviada." : $"Error Flask: {response.StatusCode}" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult GuardarTokenFcm(string tokenFcm)
        {
            var usuarioSesion = Session["Usuario"] as clsUsuario;
            if (usuarioSesion == null) return Json(new { success = false, message = "No autenticado" });
            if (string.IsNullOrEmpty(tokenFcm)) return Json(new { success = false, message = "Token inválido" });

            return Json(new { success = false, message = "Funcionalidad de guardar token FCM no conectada al repositorio actual." });
        }

        [HttpPost]
        public JsonResult ApiLogin(string correo, string contrasena)
        {
            string mensajeError;
            bool esAprobado;
            clsUsuario usuario = _repository.Autenticar(correo, contrasena, out mensajeError, out esAprobado);

            if (usuario != null)
            {
                var userData = new { id = usuario.Id, nombre = usuario.Nombre, correo = usuario.Correo, tipo = usuario.TipoUsuario };
                return Json(new { success = true, usuario = userData });
            }
            else
            {
                bool noAprobadoAun = mensajeError.Contains("espera de aceptación");
                return Json(new { success = false, message = mensajeError, notApprovedYet = noAprobadoAun });
            }
        }
        public ActionResult Mensaje() { return View(); }
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login", "Usuario");
        }
    }
}