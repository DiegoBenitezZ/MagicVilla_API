using MagicVilla_Web.Models;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace MagicVilla_Web.Services.IServices
{
    public interface IBaseService
    {
        APIResponse responseModel { get; set; }

        Task<T> SendAsync<T>(APIRequest apiRequest);
    }
}
