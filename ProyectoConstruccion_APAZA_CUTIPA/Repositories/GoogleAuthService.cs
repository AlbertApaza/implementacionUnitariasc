using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;

namespace ProyectoConstruccion_APAZA_CUTIPA.Services 
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly HttpClient _httpClient;

        public GoogleAuthService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(20);
        }

        public GoogleAuthService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<GoogleUserInfo> GetUserInfoFromGoogleCodeAsync(string code, string clientId, string clientSecret, string redirectUri)
        {
            var userInfo = new GoogleUserInfo();

            try
            {
                // 1. Intercambiar código por token de acceso
                var tokenRequestParameters = new System.Collections.Specialized.NameValueCollection
                {
                    { "code", code },
                    { "client_id", clientId },
                    { "client_secret", clientSecret },
                    { "redirect_uri", redirectUri },
                    { "grant_type", "authorization_code" }
                };

                var tokenContent = new FormUrlEncodedContent(ToKeyValuePair(tokenRequestParameters));
                HttpResponseMessage tokenResponse = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", tokenContent);

                if (!tokenResponse.IsSuccessStatusCode)
                {
                    string errorContent = await tokenResponse.Content.ReadAsStringAsync();
                    userInfo.Error = $"Error al obtener token de Google: {tokenResponse.StatusCode} - {errorContent}";
                    return userInfo;
                }

                string tokenResponseString = await tokenResponse.Content.ReadAsStringAsync();
                JObject tokenJson = JObject.Parse(tokenResponseString);
                string accessToken = tokenJson["access_token"]?.ToString();

                if (string.IsNullOrEmpty(accessToken))
                {
                    userInfo.Error = "Token de acceso de Google no recibido.";
                    return userInfo;
                }

                // 2. Obtener información del usuario usando el token de acceso
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                HttpResponseMessage userInfoResponse = await _httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");

                if (!userInfoResponse.IsSuccessStatusCode)
                {
                    string errorContent = await userInfoResponse.Content.ReadAsStringAsync();
                    userInfo.Error = $"Error al obtener información del usuario de Google: {userInfoResponse.StatusCode} - {errorContent}";
                    return userInfo;
                }

                string userInfoResponseString = await userInfoResponse.Content.ReadAsStringAsync();
                JObject userInfoJson = JObject.Parse(userInfoResponseString); 

                userInfo.Email = userInfoJson["email"]?.ToString();
                userInfo.Name = userInfoJson["name"]?.ToString();

                if (string.IsNullOrEmpty(userInfo.Email) || string.IsNullOrEmpty(userInfo.Name))
                {
                    userInfo.Error = "No se pudo obtener el email o el nombre del perfil de Google.";
                }
            }
            catch (HttpRequestException ex)
            {
                userInfo.Error = "Error de red al contactar con Google: " + ex.Message;
            }
            catch (Exception ex)
            {
                userInfo.Error = "Error inesperado durante la autenticación con Google: " + ex.Message;
            }

            return userInfo;
        }

        private static System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>> ToKeyValuePair(System.Collections.Specialized.NameValueCollection col)
        {
            foreach (string key in col.AllKeys)
            {
                yield return new System.Collections.Generic.KeyValuePair<string, string>(key, col[key]);
            }
        }
    }
}