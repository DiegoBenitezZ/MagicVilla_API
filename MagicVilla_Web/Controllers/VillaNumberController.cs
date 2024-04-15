using AutoMapper;
using MagicVilla_Utility;
using MagicVilla_Web.Models;
using MagicVilla_Web.Models.Dto;
using MagicVilla_Web.Models.ViewModels;
using MagicVilla_Web.Services;
using MagicVilla_Web.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace MagicVilla_Web.Controllers
{
	public class VillaNumberController : Controller
	{
		private readonly IVillaNumberService _villaNumberService;
		private readonly IVillaService _villaService;
		private readonly IMapper _mapper;

        public VillaNumberController(IVillaNumberService villaNumberService, IVillaService villaService, IMapper mapper)
        {
            _mapper = mapper;
			_villaNumberService = villaNumberService;
			_villaService = villaService;
        }

        public async Task<IActionResult> Index()
		{
			var response = await _villaNumberService.GetAllAsync<APIResponse>(HttpContext.Session.GetString(SD.SessionToken));

			if(response != null && response.IsSuccess)
			{
				List<VillaNumberDTO>  list = JsonConvert.DeserializeObject<List<VillaNumberDTO>>(Convert.ToString(response.Result));
				return View(list);
			}
			return NotFound();
		}

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create()
		{
			VillaNumberCreateVM villaNumberCreateVM = new();
			var response = await _villaService.GetAllAsync<APIResponse>(HttpContext.Session.GetString(SD.SessionToken));

			if (response != null && response.IsSuccess)
			{
				villaNumberCreateVM.VillaList = JsonConvert.DeserializeObject<List<VillaDTO>>
					(Convert.ToString(response.Result)).Select(i => new SelectListItem
					{
						Text = i.Name,
						Value = i.Id.ToString(),
					});
			}

			return View(villaNumberCreateVM);
		}

		[HttpPost]
        [Authorize(Roles = "admin")]
        [ActionName("Create")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreatePost(VillaNumberCreateVM model)
		{
			if (ModelState.IsValid)
			{
				var response = await _villaNumberService.CreateAsync<APIResponse>(model.VillaNumberCreateDTO, HttpContext.Session.GetString(SD.SessionToken));
				if (response != null && response.IsSuccess)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{
					if(response.ErrorMessages.Count > 0)
					{
						ModelState.AddModelError("CustomError", response.ErrorMessages.FirstOrDefault());
					}
				}
			}

			var responseList = await _villaService.GetAllAsync<APIResponse>(HttpContext.Session.GetString(SD.SessionToken));

			if (responseList != null && responseList.IsSuccess)
			{
				model.VillaList = JsonConvert.DeserializeObject<List<VillaDTO>>
					(Convert.ToString(responseList.Result)).Select(i => new SelectListItem
					{
						Text = i.Name,
						Value = i.Id.ToString(),
					});
			}

			return View(model);
		}

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update(int VillaNo)
		{
			VillaNumberUpdateVM villaNumberUpdateVM = new();
			var response = await _villaNumberService.GetAsync<APIResponse>(VillaNo, HttpContext.Session.GetString(SD.SessionToken));

			if (response != null && response.IsSuccess)
			{
				VillaNumberDTO model = JsonConvert.DeserializeObject<VillaNumberDTO>(Convert.ToString(response.Result));
				villaNumberUpdateVM.VillaNumberUpdateDTO = _mapper.Map<VillaNumberUpdateDTO>(model);
			}

			response = await _villaService.GetAllAsync<APIResponse>(HttpContext.Session.GetString(SD.SessionToken));

			if (response != null && response.IsSuccess)
			{
				villaNumberUpdateVM.VillaList = JsonConvert.DeserializeObject<List<VillaDTO>>(Convert.ToString(response.Result)).Select(i => new SelectListItem
				{
					Text = i.Name,
					Value = i.Id.ToString(),
				});

				return View(villaNumberUpdateVM);
			}

			return NotFound();
		}

		[HttpPost]
        [Authorize(Roles = "admin")]
        [ActionName("Update")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdatePost(VillaNumberUpdateVM model)
		{
			if (ModelState.IsValid)
			{
				var response = await _villaNumberService.UpdateAsync<APIResponse>(model.VillaNumberUpdateDTO, HttpContext.Session.GetString(SD.SessionToken));
				if (response != null && response.IsSuccess)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{
					if (response.ErrorMessages.Count > 0)
					{
						ModelState.AddModelError("CustomError", response.ErrorMessages.FirstOrDefault());
					}
				}
			}

			var responseList = await _villaService.GetAllAsync<APIResponse>(HttpContext.Session.GetString(SD.SessionToken));

			if (responseList != null && responseList.IsSuccess)
			{
				model.VillaList = JsonConvert.DeserializeObject<List<VillaDTO>>
					(Convert.ToString(responseList.Result)).Select(i => new SelectListItem
					{
						Text = i.Name,
						Value = i.Id.ToString(),
					});
			}

			return View(model);
		}

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int VillaNo)
		{
			VillaNumberDeleteVM villaNumberDeleteVM = new();
			var response = await _villaNumberService.GetAsync<APIResponse>(VillaNo, HttpContext.Session.GetString(SD.SessionToken));

			if(response != null && response.IsSuccess)
			{
				VillaNumberDTO villaNumberDTO = JsonConvert.DeserializeObject<VillaNumberDTO>(Convert.ToString(response.Result));
				villaNumberDeleteVM.VillaNumberDTO = villaNumberDTO;
			}

			response = await _villaService.GetAllAsync<APIResponse>(HttpContext.Session.GetString(SD.SessionToken));

			if (response != null && response.IsSuccess)
			{
				List<VillaDTO> list = JsonConvert.DeserializeObject<List<VillaDTO>>(Convert.ToString(response.Result));

				IEnumerable<SelectListItem> villas = list.Select(i => new SelectListItem()
				{
					Text = i.Name,
					Value = i.Id.ToString(),
				});

				villaNumberDeleteVM.VillaList = villas;
			}

			return View(villaNumberDeleteVM);
		}

		[HttpPost]
        [Authorize(Roles = "admin")]
        [ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeletePost(VillaNumberDeleteVM vm)
		{
			var response = await _villaNumberService.DeleteAsync<APIResponse>(vm.VillaNumberDTO.VillaNo, HttpContext.Session.GetString(SD.SessionToken));

			if (response != null && response.IsSuccess)
			{
				return RedirectToAction(nameof(Index));
			}

			var responseVillas = await _villaService.GetAllAsync<APIResponse>(HttpContext.Session.GetString(SD.SessionToken));

			if (responseVillas != null && responseVillas.IsSuccess)
			{
				List<VillaDTO> list = JsonConvert.DeserializeObject<List<VillaDTO>>(Convert.ToString(responseVillas.Result));

				IEnumerable<SelectListItem> villas = list.Select(i => new SelectListItem
				{
					Text = i.Name,
					Value = i.Id.ToString(),
				});

				vm.VillaList = villas;

				return View(vm);
			}

			return NotFound();
		}
	}
}
