using MagicVilla_Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MagicVilla_Web.Extensions
{
	public class AuthExceptionRedirection : IExceptionFilter
	{
		public void OnException(ExceptionContext context)
		{
			if (context.Exception is AuthController)
				context.Result = new RedirectToActionResult("Login", "Auth", null);
		}
	}
}
