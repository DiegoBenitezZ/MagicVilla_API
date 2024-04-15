using MagicVilla_Utility;
using MagicVilla_Web.Models;
using MagicVilla_Web.Models.Dto;
using MagicVilla_Web.Services;
using MagicVilla_Web.Services.IServices;

namespace MagicVilla_Web.Repository
{
	public class AuthService: BaseService, IAuthService
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private string villaUrl;

        public AuthService(IHttpClientFactory httpClientFactory, IConfiguration config): base(httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            villaUrl = config.GetValue<string>("ServiceUrls:VillaAPI");
        }

		public Task<T> LoginAsync<T>(LoginRequestDTO obj)
		{
			return SendAsync<T>(new APIRequest() { 
				ApiType = SD.ApiType.POST,
				Data = obj,
				Url = villaUrl + "/api/" + SD.CurrentApiVersion + "/UsersAuth/login"
			});
		}

		public Task<T> RegisterAsync<T>(RegistrationRequestDTO obj)
		{
			return SendAsync<T>(new APIRequest()
			{
				ApiType = SD.ApiType.POST,
				Data = obj,
				Url = villaUrl + "/api/" + SD.CurrentApiVersion + "/UsersAuth/register"
            });
		}
	}
}
