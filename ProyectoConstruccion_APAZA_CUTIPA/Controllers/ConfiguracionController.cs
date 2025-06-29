using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Web.Mvc;
using Newtonsoft.Json;
using ProyectoConstruccion_APAZA_CUTIPA.Models;
using ProyectoConstruccion_APAZA_CUTIPA.Filters;

namespace ProyectoConstruccion_APAZA_CUTIPA.Controllers
{
    public class ConfiguracionController : Controller
    {
        public ActionResult Index()
        {
            var usuarioSesion = Session["Usuario"] as clsUsuario;
            if (usuarioSesion == null)
            {
                return RedirectToAction("Login", "Usuario");
            }

            string mensajeError;
            List<clsContactoEmergencia> contactos = clsContactoEmergencia.ObtenerPorUsuario(usuarioSesion.Id, out mensajeError);

            if (!string.IsNullOrEmpty(mensajeError))
            {
                ViewBag.MensajeError = mensajeError;
            }

            ViewBag.ContactoPrincipalObject = contactos.FirstOrDefault(c => c.EsPrincipal);
            return View(contactos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AgregarContactoWeb(clsContactoEmergencia nuevoContacto)
        {
            var usuarioSesion = Session["Usuario"] as clsUsuario;
            if (usuarioSesion == null) { TempData["ErrorMessage"] = "Sesión inválida."; return RedirectToAction("Index"); }

            if (!string.IsNullOrWhiteSpace(nuevoContacto.ParentescoOtro) && nuevoContacto.Parentesco == "Otro")
            {
                nuevoContacto.Parentesco = nuevoContacto.ParentescoOtro;
            }

            string mensajeError;
            if (clsContactoEmergencia.Agregar(nuevoContacto, usuarioSesion.Id, out mensajeError))
            {
                TempData["SuccessMessage"] = "Contacto agregado exitosamente.";
            }
            else
            {
                TempData["ErrorMessage"] = mensajeError;
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult ObtenerContactoParaEditarWeb(int idContactoEmergencia)
        {
            var usuarioSesion = Session["Usuario"] as clsUsuario;
            if (usuarioSesion == null) { return Json(new { success = false, message = "Sesión inválida." }, JsonRequestBehavior.AllowGet); }

            string mensajeError;
            clsContactoEmergencia contacto = clsContactoEmergencia.ObtenerPorId(idContactoEmergencia, usuarioSesion.Id, out mensajeError);

            if (contacto != null)
            {
                return Json(new { success = true, data = contacto }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = false, message = string.IsNullOrEmpty(mensajeError) ? "Contacto no encontrado." : mensajeError }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarContactoWeb(clsContactoEmergencia contactoEditado)
        {
            var usuarioSesion = Session["Usuario"] as clsUsuario;
            if (usuarioSesion == null) { TempData["ErrorMessage"] = "Sesión inválida."; return RedirectToAction("Index"); }

            if (!string.IsNullOrWhiteSpace(contactoEditado.ParentescoOtro) && contactoEditado.Parentesco == "Otro")
            {
                contactoEditado.Parentesco = contactoEditado.ParentescoOtro;
            }

            string mensajeError;
            if (clsContactoEmergencia.Editar(contactoEditado, usuarioSesion.Id, out mensajeError))
            {
                TempData["SuccessMessage"] = "Contacto actualizado.";
            }
            else
            {
                TempData["ErrorMessage"] = string.IsNullOrEmpty(mensajeError) ? "No se pudo actualizar." : mensajeError;
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarcarComoPrincipalWeb(int idContactoEmergencia)
        {
            var usuarioSesion = Session["Usuario"] as clsUsuario;
            if (usuarioSesion == null) { TempData["ErrorMessage"] = "Sesión inválida."; return RedirectToAction("Index"); }

            string mensajeError;
            if (clsContactoEmergencia.MarcarComoPrincipal(idContactoEmergencia, usuarioSesion.Id, out mensajeError))
            {
                TempData["SuccessMessage"] = "Contacto marcado como principal.";
            }
            else
            {
                TempData["ErrorMessage"] = string.IsNullOrEmpty(mensajeError) ? "No se pudo marcar como principal." : mensajeError;
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarContactoWeb(int idContactoEmergencia)
        {
            var usuarioSesion = Session["Usuario"] as clsUsuario;
            if (usuarioSesion == null) { TempData["ErrorMessage"] = "Sesión inválida."; return RedirectToAction("Index"); }

            string mensajeError;
            if (clsContactoEmergencia.Eliminar(idContactoEmergencia, usuarioSesion.Id, out mensajeError))
            {
                TempData["SuccessMessage"] = "Contacto eliminado.";
            }
            else
            {
                TempData["ErrorMessage"] = string.IsNullOrEmpty(mensajeError) ? "No se pudo eliminar." : mensajeError;
            }
            return RedirectToAction("Index");
        }

        private int GetUserIdFromJwt()
        {
            var principal = User as ClaimsPrincipal;
            var userIdClaim = principal?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            Response.StatusCode = 401;
            throw new Exception("Usuario no autenticado o ID no encontrado en token.");
        }

        [Route("api/contactos/listar")]
        [HttpGet]
        [JwtAuthenticationFilter]
        public JsonResult ListarContactosApi()
        {
            try
            {
                int idUsuario = GetUserIdFromJwt();
                string mensajeError;
                List<clsContactoEmergencia> contactos = clsContactoEmergencia.ObtenerPorUsuario(idUsuario, out mensajeError);
                if (!string.IsNullOrEmpty(mensajeError))
                {
                    return Json(new { success = false, message = mensajeError }, JsonRequestBehavior.AllowGet);
                }
                return Json(contactos, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                if (Response.StatusCode == 401) return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
                return Json(new { success = false, message = "Error al listar contactos (API): " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [Route("api/contactos/agregar")]
        [HttpPost]
        [JwtAuthenticationFilter]
        public JsonResult AgregarContactoApi()
        {
            try
            {
                Request.InputStream.Seek(0, SeekOrigin.Begin);
                string jsonData = new StreamReader(Request.InputStream).ReadToEnd();
                clsContactoEmergencia nuevoContacto = JsonConvert.DeserializeObject<clsContactoEmergencia>(jsonData);

                if (nuevoContacto == null) return Json(new { success = false, message = "Datos del contacto no recibidos." });

                int idUsuario = GetUserIdFromJwt();
                if (!string.IsNullOrWhiteSpace(nuevoContacto.ParentescoOtro) && nuevoContacto.Parentesco == "Otro")
                {
                    nuevoContacto.Parentesco = nuevoContacto.ParentescoOtro;
                }

                string mensajeError;
                if (clsContactoEmergencia.Agregar(nuevoContacto, idUsuario, out mensajeError))
                {
                    return Json(new { success = true, message = "Contacto agregado." });
                }
                return Json(new { success = false, message = mensajeError });
            }
            catch (Exception ex)
            {
                if (Response.StatusCode == 401) return Json(new { success = false, message = ex.Message });
                return Json(new { success = false, message = "Error al agregar contacto (API): " + ex.Message });
            }
        }

        [Route("api/contactos/obtener/{id}")]
        [HttpGet]
        [JwtAuthenticationFilter]
        public JsonResult ObtenerContactoApi(int id)
        {
            try
            {
                int idUsuario = GetUserIdFromJwt();
                string mensajeError;
                clsContactoEmergencia contacto = clsContactoEmergencia.ObtenerPorId(id, idUsuario, out mensajeError);

                if (contacto != null) return Json(new { success = true, data = contacto }, JsonRequestBehavior.AllowGet);
                return Json(new { success = false, message = string.IsNullOrEmpty(mensajeError) ? "Contacto no encontrado." : mensajeError }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                if (Response.StatusCode == 401) return Json(new { success = false, message = ex.Message });
                return Json(new { success = false, message = "Error al obtener contacto (API): " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [Route("api/contactos/editar")]
        [HttpPost]
        [JwtAuthenticationFilter]
        public JsonResult EditarContactoApi()
        {
            try
            {
                Request.InputStream.Seek(0, SeekOrigin.Begin);
                string jsonData = new StreamReader(Request.InputStream).ReadToEnd();
                clsContactoEmergencia contactoEditado = JsonConvert.DeserializeObject<clsContactoEmergencia>(jsonData);

                if (contactoEditado == null) return Json(new { success = false, message = "Datos del contacto no recibidos." });

                int idUsuario = GetUserIdFromJwt();
                if (!string.IsNullOrWhiteSpace(contactoEditado.ParentescoOtro) && contactoEditado.Parentesco == "Otro")
                {
                    contactoEditado.Parentesco = contactoEditado.ParentescoOtro;
                }

                string mensajeError;
                if (clsContactoEmergencia.Editar(contactoEditado, idUsuario, out mensajeError))
                {
                    return Json(new { success = true, message = "Contacto actualizado." });
                }
                return Json(new { success = false, message = string.IsNullOrEmpty(mensajeError) ? "No se pudo actualizar." : mensajeError });
            }
            catch (Exception ex)
            {
                if (Response.StatusCode == 401) return Json(new { success = false, message = ex.Message });
                return Json(new { success = false, message = "Error al editar contacto (API): " + ex.Message });
            }
        }

        [Route("api/contactos/marcarprincipal/{id}")]
        [HttpPost]
        [JwtAuthenticationFilter]
        public JsonResult MarcarComoPrincipalApi(int id)
        {
            try
            {
                int idUsuario = GetUserIdFromJwt();
                string mensajeError;
                if (clsContactoEmergencia.MarcarComoPrincipal(id, idUsuario, out mensajeError))
                {
                    return Json(new { success = true, message = "Marcado como principal." });
                }
                return Json(new { success = false, message = string.IsNullOrEmpty(mensajeError) ? "No se pudo marcar como principal." : mensajeError });
            }
            catch (Exception ex)
            {
                if (Response.StatusCode == 401) return Json(new { success = false, message = ex.Message });
                return Json(new { success = false, message = "Error al marcar como principal (API): " + ex.Message });
            }
        }

        [Route("api/contactos/eliminar/{id}")]
        [HttpPost]
        [JwtAuthenticationFilter]
        public JsonResult EliminarContactoApi(int id)
        {
            try
            {
                int idUsuario = GetUserIdFromJwt();
                string mensajeError;
                if (clsContactoEmergencia.Eliminar(id, idUsuario, out mensajeError))
                {
                    return Json(new { success = true, message = "Contacto eliminado." });
                }
                return Json(new { success = false, message = string.IsNullOrEmpty(mensajeError) ? "No se pudo eliminar." : mensajeError });
            }
            catch (Exception ex)
            {
                if (Response.StatusCode == 401) return Json(new { success = false, message = ex.Message });
                return Json(new { success = false, message = "Error al eliminar contacto (API): " + ex.Message });
            }
        }
    }
}