using MagicVilla_Utility;
using MagicVilla_Web.Models;
using MagicVilla_Web.Models.Dto;
using MagicVilla_Web.Services;
using MagicVilla_Web.Services.IServices;

namespace MagicVilla_Web.Repository
{
	public class AuthService: IAuthService
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IBaseService _baseService;
		private string villaUrl;

        public AuthService(IHttpClientFactory httpClientFactory, IConfiguration config, IBaseService baseService)
        {
            _baseService = baseService;
            _httpClientFactory = httpClientFactory;
			villaUrl = config.GetValue<string>("ServiceUrls:VillaAPI");
        }

		public async Task<T> LoginAsync<T>(LoginRequestDTO obj)
		{
			return await _baseService.SendAsync<T>(new APIRequest() { 
				ApiType = SD.ApiType.POST,
				Data = obj,
				Url = villaUrl + "/api/" + SD.CurrentApiVersion + "/UsersAuth/login"
			}, withBearer: false);
		}

		public async Task<T> RegisterAsync<T>(RegistrationRequestDTO obj)
		{
			return await _baseService.SendAsync<T>(new APIRequest()
			{
				ApiType = SD.ApiType.POST,
				Data = obj,
				Url = villaUrl + "/api/" + SD.CurrentApiVersion + "/UsersAuth/register"
            }, withBearer: false);
		}

		public async Task<T> LogoutAsync<T>(TokenDTO obj)
		{
			return await _baseService.SendAsync<T>(new APIRequest()
			{
				ApiType = SD.ApiType.POST,
				Data = obj,
				Url = villaUrl + "/api/" + SD.CurrentApiVersion + "/UsersAuth/revoke"
			});
		}
	}
}
