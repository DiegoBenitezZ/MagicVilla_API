using MagicVilla_Utility;
using MagicVilla_Web.Models;
using MagicVilla_Web.Models.Dto;
using MagicVilla_Web.Services.IServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using static MagicVilla_Utility.SD;

namespace MagicVilla_Web.Services
{
	public class BaseService : IBaseService
	{
		public APIResponse responseModel { get; set; }
		public IHttpClientFactory httpClient { get; set; }
		private readonly ITokenProvider _tokenProvider;
		private readonly IApiMessageRequestBuilder _apiMessageRequestBuilder;
		protected readonly string VillaApiUrl;
		private IHttpContextAccessor _contextAccessor;

		public BaseService(IHttpClientFactory httpClient, ITokenProvider tokenProvider, IConfiguration config, IHttpContextAccessor contextAccessor,
			IApiMessageRequestBuilder apiMessageRequestBuilder)
		{
			this.responseModel = new();
			this.httpClient = httpClient;
			_tokenProvider = tokenProvider;
			_contextAccessor = contextAccessor;
			_apiMessageRequestBuilder = apiMessageRequestBuilder;
			VillaApiUrl = config.GetValue<string>("ServiceUrls:VillaAPI");
		}

		public async Task<T> SendAsync<T>(APIRequest apiRequest, bool withBearer = true)
		{
			try
			{
				var client = httpClient.CreateClient("MagicAPI");
				TokenDTO tokeDTO = _tokenProvider.GetToken();

				var messageFactory = () =>
				{
					return _apiMessageRequestBuilder.Build(apiRequest);
				};

				HttpResponseMessage httpResponseMessage = null;

				httpResponseMessage = await this.SendWithRefreshToken(client, messageFactory, withBearer);

				APIResponse FinalApiResponse = new()
				{
					IsSuccess = false,
				};

				try
				{
					switch(httpResponseMessage.StatusCode)
					{
						case HttpStatusCode.NotFound:
							FinalApiResponse.ErrorMessages = new List<string>() { "Not Found" };
							break;
						case HttpStatusCode.Forbidden:
							FinalApiResponse.ErrorMessages = new List<string>() { "Access Denied" };
							break;
						case HttpStatusCode.Unauthorized:
							FinalApiResponse.ErrorMessages = new List<string>() { "Unauthorized" };
							break;
						case HttpStatusCode.InternalServerError:
							FinalApiResponse.ErrorMessages = new List<string>() { "Internal Server Error" };
							break;
						default:
							var apiContent = await httpResponseMessage.Content.ReadAsStringAsync();
							FinalApiResponse.IsSuccess = true;
							FinalApiResponse = JsonConvert.DeserializeObject<APIResponse>(apiContent);
							break;
					}
				}
				catch (Exception e)
				{
					FinalApiResponse.ErrorMessages = new List<string>() { "Error Encountered", e.Message.ToString() };
				}

				var res = JsonConvert.SerializeObject(FinalApiResponse);
				var returnObj = JsonConvert.DeserializeObject<T>(res);
				return returnObj;
			}
			catch(AuthException)
			{
				throw;
			}
			catch (Exception e)
			{
				var dto = new APIResponse
				{
					ErrorMessages = new List<string> { Convert.ToString(e.Message) },
					IsSuccess = false
				};
				var res = JsonConvert.SerializeObject(dto);
				var APIResponse = JsonConvert.DeserializeObject<T>(res);
				return APIResponse;
			}
		}

		private async Task<HttpResponseMessage> SendWithRefreshToken(HttpClient httpClient,
			Func<HttpRequestMessage> httpRequestMessageFactory, bool withBearer = true)
		{
			if(!withBearer)
			{
				return await httpClient.SendAsync(httpRequestMessageFactory());
			}
			else
			{
				TokenDTO tokenDTO = _tokenProvider.GetToken();

				if(tokenDTO != null && !string.IsNullOrEmpty(tokenDTO.AccessToken))
				{
					httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenDTO.AccessToken);
				}

				try
				{
					var response = await httpClient.SendAsync(httpRequestMessageFactory());

					if(response.IsSuccessStatusCode)
					{
						return response;
					}

					if(!response.IsSuccessStatusCode && response.StatusCode == HttpStatusCode.Unauthorized)
					{
						await InvokeRefreshTokenEndpoint(httpClient, tokenDTO.AccessToken, tokenDTO.RefreshToken);
						response = await httpClient.SendAsync(httpRequestMessageFactory());

						return response;
					}

					return response;
				}
				catch (AuthException)
				{
					throw;
				}
				catch (HttpRequestException httpRequestException)
				{
					if(httpRequestException.StatusCode == HttpStatusCode.Unauthorized)
					{
						await InvokeRefreshTokenEndpoint(httpClient, tokenDTO.AccessToken, tokenDTO.RefreshToken);
						return await httpClient.SendAsync(httpRequestMessageFactory());

					}

					throw;
				}
			}
		}

		private async Task InvokeRefreshTokenEndpoint(HttpClient httpClient, string existingAccessToken, string existingRefreshToken)
		{
			HttpRequestMessage message = new();

			message.Headers.Add("Accept", "application/json");
			message.RequestUri = new Uri($"{VillaApiUrl}/api/{SD.CurrentApiVersion}/UsersAuth/refresh");
			message.Method = HttpMethod.Post;
			message.Content = new StringContent(JsonConvert.SerializeObject(new TokenDTO()
			{
				AccessToken = existingAccessToken,
				RefreshToken = existingRefreshToken
			}), Encoding.UTF8, "application/json");

			var response = await httpClient.SendAsync(message);
			var content = await response.Content.ReadAsStringAsync();
			var apiResponse = JsonConvert.DeserializeObject<APIResponse>(content);

			if(apiResponse.IsSuccess != true)
			{
				await _contextAccessor.HttpContext.SignOutAsync();
				_tokenProvider.ClearToken();

				throw new AuthException();
			}
			else
			{
				var tokenDataStr = JsonConvert.SerializeObject(apiResponse.Result);
				var tokenDTO = JsonConvert.DeserializeObject<TokenDTO>(tokenDataStr);

				if(tokenDTO != null && !string.IsNullOrEmpty(tokenDTO.AccessToken)) {
					await SignInWithNewTokens(tokenDTO);
					httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenDTO.AccessToken);
				}
			}
		}

		private async Task SignInWithNewTokens(TokenDTO tokenDTO)
		{
			var handler = new JwtSecurityTokenHandler();
			var jwt = handler.ReadJwtToken(tokenDTO.AccessToken);
			var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
			
			identity.AddClaim(new Claim(ClaimTypes.Name, jwt.Claims.FirstOrDefault(u => u.Type == "unique_name").Value));
			identity.AddClaim(new Claim(ClaimTypes.Role, jwt.Claims.FirstOrDefault(u => u.Type == "role").Value));

			var principal = new ClaimsPrincipal(identity);

			await _contextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

			_tokenProvider.SetToken(tokenDTO);
		}
	}
}
