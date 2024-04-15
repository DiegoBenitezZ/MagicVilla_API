using MagicVilla_Web.Models.Dto;

namespace MagicVilla_Web.Services.IServices
{
	public interface IAuthService: IBaseService
	{
		Task<T> LoginAsync<T>(LoginRequestDTO obj);
		Task<T> RegisterAsync<T>(RegistrationRequestDTO obj);
	}
}
