using ProyectoConstruccion_APAZA_CUTIPA.Models;
using System;
using System.Web.Mvc;

namespace ProyectoConstruccion_APAZA_CUTIPA.Controllers
{
    public class CamaraController : Controller
    {
        // GET: Camara
        public ActionResult Index()
        {
            var viewModel = new CamaraViewModel
            {
                Camara = new clsCamara
                {
                    IdCamara = "CAM001",
                    Ubicacion = "Entrada Principal",
                    Estado = "Conectado"
                },
                Contador = new clsContadorPersona
                {
                    CantidadPersonas = 1,
                    CantidadObjetosPeligrosos = 3,
                    HoraActual = DateTime.Now
                }
            };

            return View(viewModel);
        }

        public ActionResult Iniciar()
        {
            var camara = new clsCamara
            {
                IdCamara = "CAM001",
                Ubicacion = "Entrada Principal"
            };

            camara.IniciarTransmision();

            var viewModel = new CamaraViewModel
            {
                Camara = camara,
                Contador = new clsContadorPersona
                {
                    CantidadPersonas = 0,
                    CantidadObjetosPeligrosos = 0,
                    HoraActual = DateTime.Now
                }
            };

            return View("Index", viewModel);
        }

        public ActionResult Detener()
        {
            var camara = new clsCamara
            {
                IdCamara = "CAM001",
                Ubicacion = "Entrada Principal"
            };

            camara.DetenerTransmision();

            var viewModel = new CamaraViewModel
            {
                Camara = camara,
                Contador = new clsContadorPersona
                {
                    CantidadPersonas = 0,
                    CantidadObjetosPeligrosos = 0,
                    HoraActual = DateTime.Now
                }
            };

            return View("Index", viewModel);
        }
    }
}
