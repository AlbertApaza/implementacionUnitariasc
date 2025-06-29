using System.Configuration;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using Owin;
using Microsoft.AspNet.Identity;

[assembly: OwinStartup(typeof(ProyectoConstruccion_APAZA_CUTIPA.Startup))]

namespace ProyectoConstruccion_APAZA_CUTIPA
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Configuración de cookies
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Usuario/Login"),
                ExpireTimeSpan = System.TimeSpan.FromMinutes(60),
                SlidingExpiration = true
            });

            // Configuración para login externo
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // Configuración de Google OAuth
            var googleClientId = ConfigurationManager.AppSettings["GoogleClientId"];
            var googleClientSecret = ConfigurationManager.AppSettings["GoogleClientSecret"];

            if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
            {
                app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions()
                {
                    ClientId = googleClientId,
                    ClientSecret = googleClientSecret,
                    CallbackPath = new PathString("/Usuario/ExternalLoginCallback"),
                    SignInAsAuthenticationType = DefaultAuthenticationTypes.ExternalCookie,
                    Scope = { "email", "profile" }, // Asegurar que tenemos acceso al email y perfil
                    Provider = new GoogleOAuth2AuthenticationProvider()
                    {
                        OnAuthenticated = (context) =>
                        {
                            // Agregar claims adicionales si es necesario
                            context.Identity.AddClaim(new System.Security.Claims.Claim("urn:google:email", context.Email));
                            context.Identity.AddClaim(new System.Security.Claims.Claim("urn:google:name", context.Name));
                            return System.Threading.Tasks.Task.FromResult(0);
                        }
                    }
                });
            }
        }
    }
}