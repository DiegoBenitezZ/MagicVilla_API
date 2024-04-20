using Microsoft.AspNetCore.Diagnostics;
using Newtonsoft.Json;

namespace MagicVilla_VillaAPI.Middlewares
{
	public class CustomExceptionMiddleware
	{
		private readonly RequestDelegate _requestDelegate;

        public CustomExceptionMiddleware(RequestDelegate requestDelegate)
        {
            _requestDelegate = requestDelegate;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _requestDelegate(context);
            }
            catch (Exception ex)
            {
                await ProccessException(context, ex);
            }
        }

		private async Task ProccessException(HttpContext context, Exception ex)
		{
			context.Response.StatusCode = 500;
			context.Response.ContentType = "application/json";
			
			if (ex is BadImageFormatException badImageFormatException)
			{
				await context.Response.WriteAsync(JsonConvert.SerializeObject(new
				{
					StatusCode = 776,
					ErrorMessage = "Hello From Custom Handler! Image Format is invalid"
				}));
			}
			else
			{
				await context.Response.WriteAsync(JsonConvert.SerializeObject(new
				{
					Statuscode = context.Response.StatusCode,
					ErrorMessage = "Hello From Middleware! - Finale"
				}));
				}
		}
	}
}
