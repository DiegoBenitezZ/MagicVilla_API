using AutoMapper;
using MagicVilla_VillaAPI.Models;
using MagicVilla_VillaAPI.Models.Dto;
using MagicVilla_VillaAPI.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace MagicVilla_VillaAPI.Controllers.v1
{
    [Route("api/v{version:apiVersion}/villaNumberAPI")]
    [ApiController]
    [ApiVersion("1.0")]
    public class VillaNumberApiController : ControllerBase
    {
        protected APIResponse _response;
        private readonly IVillaNumberRepository _dbVillaNumber;
        private readonly IVillaRepository _dbVilla;
        private readonly IMapper _mapper;

        public VillaNumberApiController(IMapper mapper, IVillaNumberRepository dbVillaNumber, IVillaRepository dbVilla)
        {
            _mapper = mapper;
            _dbVillaNumber = dbVillaNumber;
            _dbVilla = dbVilla;
        }

        [HttpGet]
        //[MapToApiVersion("1.0")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> GetAll()
        {
            try
            {
                IEnumerable<VillaNumber> villaNumbers = await _dbVillaNumber.GetAllAsync(includeProperties: "Villa");

                _response = new()
                {
                    StatusCode = HttpStatusCode.OK,
                    Result = villaNumbers
                };

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response = new()
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    IsSuccess = false,
                    ErrorMessages = new List<string>() { ex.ToString() }
                };
            }

            return _response;
        }

        [HttpGet("getString")]
        public IEnumerable<string> Get()
        {
            return new string[] { "Diego", "Benitez" };
        }

        [HttpGet("{villaNo:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> Get(int villaNo)
        {
            try
            {
                if (villaNo <= 0)
                {
                    _response = new()
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        IsSuccess = false
                    };

                    return BadRequest(_response);
                }

                VillaNumber villaNumber = await _dbVillaNumber.GetAsync(u => u.VillaNo == villaNo, includeProperties: "Villa");

                if (villaNumber == null)
                {
                    _response = new()
                    {
                        StatusCode = HttpStatusCode.NotFound,
                        IsSuccess = false
                    };

                    return NotFound(_response);
                }

                _response = new()
                {
                    StatusCode = HttpStatusCode.OK,
                    Result = villaNumber
                };

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response = new()
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    IsSuccess = false,
                    ErrorMessages = new List<string>() { ex.ToString() }
                };
            }

            return _response;
        }


        [HttpPost]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> Create([FromBody] VillaNumberCreateDTO villaNumberCreateDTO)
        {
            try
            {
                if (villaNumberCreateDTO == null)
                {
                    _response = new()
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        IsSuccess = false
                    };

                    return BadRequest(_response);
                }

                if (await _dbVillaNumber.GetAsync(u => u.VillaNo == villaNumberCreateDTO.VillaNo) != null)
                {
                    ModelState.AddModelError("ErrorMessages", "Villa ID is Invalid!");
                    return BadRequest(ModelState);
                }

                VillaNumber villaNumber = _mapper.Map<VillaNumber>(villaNumberCreateDTO);
                villaNumber.CreatedDate = DateTime.Now;

                await _dbVillaNumber.CreateAsync(villaNumber);

                _response = new()
                {
                    StatusCode = HttpStatusCode.Created,
                };

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response = new()
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    IsSuccess = false,
                    ErrorMessages = new List<string>() { ex.ToString() }
                };
            }

            return _response;
        }

        [HttpDelete("{villaNo:int}")]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> Remove(int villaNo)
        {
            try
            {
                if (villaNo <= 0)
                {
                    _response = new()
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        IsSuccess = false
                    };

                    return BadRequest(_response);
                }

                VillaNumber villaNumber = await _dbVillaNumber.GetAsync(u => u.VillaNo == villaNo);

                if (villaNumber == null)
                {
                    _response = new()
                    {
                        StatusCode = HttpStatusCode.NotFound,
                        IsSuccess = false
                    };

                    return NotFound(_response);
                }

                _dbVillaNumber.RemoveAsync(villaNumber);
                _response = new()
                {
                    StatusCode = HttpStatusCode.NoContent,
                };


                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response = new()
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    IsSuccess = false,
                    ErrorMessages = new List<string>() { ex.ToString() }
                };
            }

            return _response;
        }

        [HttpPut("{villaNo:int}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<APIResponse>> Update(int villaNo, [FromBody] VillaNumberUpdateDTO villaNumberUpdateDTO)
        {
            try
            {
                if (villaNumberUpdateDTO == null || villaNo != villaNumberUpdateDTO.VillaNo)
                {
                    _response = new()
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        IsSuccess = false
                    };

                    return BadRequest(_response);
                }

                VillaNumber villaNumber = _mapper.Map<VillaNumber>(villaNumberUpdateDTO);


                _dbVillaNumber.UpdateAsync(villaNumber);

                _response = new()
                {
                    StatusCode = HttpStatusCode.NoContent,
                };

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response = new()
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    IsSuccess = false,
                    ErrorMessages = new List<string>() { ex.ToString() }
                };
            }

            return _response;
        }
    }
}
