using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace ProyectoConstruccion_APAZA_CUTIPA.Services
{
    public class GoogleUserInfo
    {
        public string Email { get; set; }
        public string Name { get; set; }
        public string Error { get; set; }
    }

    public interface IGoogleAuthService
    {
        Task<GoogleUserInfo> GetUserInfoFromGoogleCodeAsync(string code, string clientId, string clientSecret, string redirectUri);
    }
}