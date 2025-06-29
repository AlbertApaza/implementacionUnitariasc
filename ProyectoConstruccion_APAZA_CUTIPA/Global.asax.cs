using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace ProyectoConstruccion_APAZA_CUTIPA
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }

        protected void Application_PostAuthenticateRequest(Object sender, EventArgs e)
        {
            if (HttpContext.Current.User.Identity.IsAuthenticated)
            {
                if (HttpContext.Current.Session != null)
                {
                    if (HttpContext.Current.Session["Usuario"] == null)
                    {
                        string correo = HttpContext.Current.User.Identity.Name;
                        var usuario = new ProyectoConstruccion_APAZA_CUTIPA.Models.clsUsuario
                        {
                            Correo = correo
                        };
                        HttpContext.Current.Session["Usuario"] = usuario;
                    }
                }
            }
        }
    }
}