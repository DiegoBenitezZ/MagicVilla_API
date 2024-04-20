using AutoMapper;
using MagicVilla_Utility;
using MagicVilla_Web.Models;
using MagicVilla_Web.Models.Dto;
using MagicVilla_Web.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace MagicVilla_Web.Controllers
{
    public class VillaController : Controller
    {
        private readonly IVillaService _villaService;
        private readonly IMapper _mapper;

        public VillaController(IVillaService villaService, IMapper mapper)
        {
            _mapper = mapper;
            _villaService = villaService;
        }

        public async Task<IActionResult> Index()
        {
            List<VillaDTO> list = new();

            var response = await _villaService.GetAllAsync<APIResponse>();

            if(response != null && response.IsSuccess)
            {
                list = JsonConvert.DeserializeObject<List<VillaDTO>>(Convert.ToString(response.Result));
            }

            return View(list);
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create(VillaCreateDTO model)
        {
            return View();
        }


		[HttpPost]
        [Authorize(Roles = "admin")]
        [ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(VillaCreateDTO model)
        {
            if (ModelState.IsValid)
            {
                var response = await _villaService.CreateAsync<APIResponse>(model);
                if(response != null && response.IsSuccess)
                {
                    TempData["success"] = "Villa created successfully";
                    return RedirectToAction(nameof(Index));
                }
            }

			TempData["error"] = "Error Encountered.";
			return View(model);
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update(int villaId)
        {
            var response = await _villaService.GetAsync<APIResponse>(villaId);

            if(response != null && response.IsSuccess)
            {
                VillaDTO model = JsonConvert.DeserializeObject<VillaDTO>(Convert.ToString(response.Result));
                return View(_mapper.Map<VillaUpdateDTO>(model));
            }

            return NotFound();
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        [ActionName("Update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePost(VillaUpdateDTO model) 
        {
			if (ModelState.IsValid)
			{
				var response = await _villaService.UpdateAsync<APIResponse>(model);

				if (response != null && response.IsSuccess)
				{
					TempData["success"] = "Villa updated successfully";
					return RedirectToAction(nameof(Index));
				}
			}

			TempData["error"] = "Error Encountered.";
			return View(model);
		}

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int villaId)
		{
			var response = await _villaService.GetAsync<APIResponse>(villaId);

			if (response != null && response.IsSuccess)
			{
				VillaDTO model = JsonConvert.DeserializeObject<VillaDTO>(Convert.ToString(response.Result));
				return View(model);
			}

			return NotFound();
		}

		[HttpPost]
        [Authorize(Roles = "admin")]
        [ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeletePost(VillaDTO model)
		{
			if (ModelState.IsValid)
			{
				var response = await _villaService.DeleteAsync<APIResponse>(model.Id);

				if (response != null && response.IsSuccess)
				{
					TempData["success"] = "Villa deleted successfully";
					return RedirectToAction(nameof(Index));
				}
			}

			TempData["error"] = "Error Encountered.";
			return View(model);
		}
	}
}
