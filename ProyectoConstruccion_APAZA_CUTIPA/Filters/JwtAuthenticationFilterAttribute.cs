using System;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Web;
using System.Web.Mvc; // Para AuthorizeAttribute y AuthorizationContext
using Microsoft.IdentityModel.Tokens;

namespace ProyectoConstruccion_APAZA_CUTIPA.Filters // Asegúrate que este namespace sea correcto
{
    public class JwtAuthenticationFilterAttribute : AuthorizeAttribute
    {
        private readonly string secretKey = ConfigurationManager.AppSettings["JwtSecretKey"];
        private readonly string issuer = ConfigurationManager.AppSettings["JwtIssuer"];
        private readonly string audience = ConfigurationManager.AppSettings["JwtAudience"];

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var request = httpContext.Request;
            var authorizationHeader = request.Headers["Authorization"];

            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            var token = authorizationHeader.Substring("Bearer ".Length).Trim();
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(secretKey);
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ClockSkew = TimeSpan.Zero
                };
                SecurityToken validatedToken;
                ClaimsPrincipal principal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);
                httpContext.User = principal;
                return true;
            }
            catch
            {
                return false;
            }
        }
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            filterContext.Result = new JsonResult
            {
                Data = new { success = false, message = "No autorizado: Token inválido o ausente." },
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
            filterContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        }
    }
}