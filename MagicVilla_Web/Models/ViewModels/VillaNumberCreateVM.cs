using MagicVilla_Web.Models.Dto;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MagicVilla_Web.Models.ViewModels
{
	public class VillaNumberCreateVM
	{
        public VillaNumberCreateVM()
        {
			VillaNumberCreateDTO = new VillaNumberCreateDTO();
		}

        public VillaNumberCreateDTO VillaNumberCreateDTO { get; set; }
        [ValidateNever]
        public IEnumerable<SelectListItem> VillaList { get; set; }
    }
}
