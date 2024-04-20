using AutoMapper;
using MagicVilla_Utility;
using MagicVilla_VillaAPI.Data;
using MagicVilla_VillaAPI.Models;
using MagicVilla_VillaAPI.Models.Dto;
using MagicVilla_VillaAPI.Repository.IRepository;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MagicVilla_VillaAPI.Repository
{
	public class UserRepository : IUserRepository
	{
		private readonly ApplicationDbContext _db;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly RoleManager<IdentityRole> _roleManager;
		private readonly IMapper _mapper;
		private string secretKey;

        public UserRepository(ApplicationDbContext db, IConfiguration configuration,
			 UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IMapper mapper)
        {
            _db = db;
			_userManager = userManager;
			_roleManager = roleManager;
			_mapper = mapper;
			secretKey = configuration.GetValue<string>("ApiSettings:Secret");
        }

        public bool IsUniqueUser(string username)
		{
			var user = _db.ApplicationUsers.FirstOrDefault(u => u.UserName == username);
			
			return (user == null) ? true : false;
		}

		public async Task<TokenDTO> Login(LoginRequestDTO loginRequestDTO)
		{
			var user = _db.ApplicationUsers.FirstOrDefault(u => u.UserName.ToLower() == loginRequestDTO.UserName.ToLower());

			bool isValid = await _userManager.CheckPasswordAsync(user, loginRequestDTO.Password);

			if(user == null || !isValid)
			{
				return  new TokenDTO()
				{
					AccessToken = "",
				};
			}

			var jwtTokenId = $"JTI{Guid.NewGuid()}";
			var accessToken = await this.GetAccessToken(user, jwtTokenId);
			var refreshToken = await this.CreateNewRefreshToken(user.Id, jwtTokenId);

			TokenDTO tokenDTO = new TokenDTO()
			{
				AccessToken = accessToken,
				RefreshToken = refreshToken,
			};

			return tokenDTO;
		}

		public async Task<UserDTO> Register(RegistrationRequestDTO registrationRequestDTO)
		{
			ApplicationUser user = new()
			{
				UserName = registrationRequestDTO.UserName,
				Email = registrationRequestDTO.UserName,
				NormalizedEmail = registrationRequestDTO.UserName.ToUpper(),
				Name = registrationRequestDTO.Name,
			};

			try
			{
				var result = await _userManager.CreateAsync(user, registrationRequestDTO.Password);

				if(result.Succeeded)
				{
					if(!_roleManager.RoleExistsAsync(registrationRequestDTO.Role).GetAwaiter().GetResult())
					{
						await _roleManager.CreateAsync(new IdentityRole(registrationRequestDTO.Role));
					}

					await _userManager.AddToRoleAsync(user, registrationRequestDTO.Role);
					var userToReturn = _db.ApplicationUsers.FirstOrDefault(u => u.UserName == registrationRequestDTO.UserName);

					return _mapper.Map<UserDTO>(userToReturn);
				}
			}
			catch (Exception ex) { 
			
			}

			return new UserDTO();
		}

		public async Task<string> GetAccessToken(ApplicationUser user, string jwtTokenId)
		{
			var roles = await _userManager.GetRolesAsync(user);
			var tokenHandler = new JwtSecurityTokenHandler();
			var key = Encoding.ASCII.GetBytes(secretKey);

			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(new Claim[]
				{
					new Claim(ClaimTypes.Name, user.Name.ToString()),
					new Claim(ClaimTypes.Role, roles.FirstOrDefault()),
					new Claim(JwtRegisteredClaimNames.Jti, jwtTokenId),
					new Claim(JwtRegisteredClaimNames.Sub, user.Id)
				}),
				Expires = DateTime.UtcNow.AddMinutes(1),
				SigningCredentials = new(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
				Issuer = "https://magicvilla-api.com",
				Audience = "https://test-magic-api.com"
			};

			var token = tokenHandler.CreateToken(tokenDescriptor);

			return tokenHandler.WriteToken(token);
		}

		public async Task<TokenDTO> RefreshAccessToken(TokenDTO tokenDTO)
		{
			// Find an existing refresh Token
			RefreshToken existingRefreshToken = await _db.RefreshTokens.FirstOrDefaultAsync(u => u.Refresh_Token == tokenDTO.RefreshToken);

			if(existingRefreshToken == null)
			{
				return new TokenDTO();
			}

			// Compare data from existing refresh and access token provided and if there is any missmatch then consider it as a freud

			var isValidToken = this.GetAccessTokenData(tokenDTO.AccessToken, existingRefreshToken.UserID, existingRefreshToken.JwtTokenId);

			if(!isValidToken)
			{
				await this.MarkTokenAsInvalid(existingRefreshToken);

				return new TokenDTO();
			}

			// When someone tries to use not valid token, fraud possible 

			if(!existingRefreshToken.IsValid)
			{
				await MarkAllTokenInChainAsInvalid(existingRefreshToken.UserID, existingRefreshToken.JwtTokenId);

				return new TokenDTO();
			}


			// If just expired then mark as invalid and return empty

			if (existingRefreshToken.ExpiresAt < DateTime.UtcNow)
			{
				await this.MarkTokenAsInvalid(existingRefreshToken);

				return new TokenDTO();
			}

			// replace old refresh with a new one with updated expire date

			var newRefreshToken = await this.CreateNewRefreshToken(existingRefreshToken.UserID, existingRefreshToken.JwtTokenId);

			// revoke existing refresh token
			await this.MarkTokenAsInvalid(existingRefreshToken);

			// generate new access token
			var applicationUser = _db.ApplicationUsers.FirstOrDefault(u => u.Id == existingRefreshToken.UserID);

			if(applicationUser == null)
			{
				return new TokenDTO();
			}

			var newAccessToken = await this.GetAccessToken(applicationUser, existingRefreshToken.JwtTokenId);

			return new TokenDTO()
			{
				AccessToken = newAccessToken,
				RefreshToken = newRefreshToken,
			};
		}

		public async Task RevokeRefreshToken(TokenDTO tokenDTO)
		{
			var existingRefreshToken = await _db.RefreshTokens.FirstOrDefaultAsync(u => u.Refresh_Token == tokenDTO.RefreshToken);

			if (existingRefreshToken == null)
				return;

			var isValidToken = this.GetAccessTokenData(tokenDTO.AccessToken, existingRefreshToken.UserID, existingRefreshToken.JwtTokenId);

			if (!isValidToken)
			{
				return;
			}

			await MarkAllTokenInChainAsInvalid(existingRefreshToken.UserID, existingRefreshToken.JwtTokenId);

		}

		private async Task<string> CreateNewRefreshToken(string userId, string tokenId)
		{
			RefreshToken refreshToken = new()
			{
				IsValid = true,
				UserID = userId,
				JwtTokenId = tokenId,
				ExpiresAt = DateTime.UtcNow.AddMinutes(5),
				Refresh_Token = Guid.NewGuid() + "-" + Guid.NewGuid(),
			};

			await _db.RefreshTokens.AddAsync(refreshToken);
			await _db.SaveChangesAsync();

			return refreshToken.Refresh_Token;
		}

		public bool GetAccessTokenData(string accessToken, string expectedUserID, string expectedTokenId)
		{
			try
			{
				var tokenHandler = new JwtSecurityTokenHandler();
				var jwt = tokenHandler.ReadJwtToken(accessToken);
				var jwtTokenId = jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Jti).Value;
				var userId = jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Sub).Value;

				return (userId == expectedTokenId && jwtTokenId == expectedTokenId);

			}
			catch (Exception ex)
			{
				return false;
			}
		}

		private async Task MarkAllTokenInChainAsInvalid(string userId, string  tokenId)
		{
			var chainRecords = _db.RefreshTokens.Where(u => u.UserID == userId &&
				u.JwtTokenId == tokenId)
					.ExecuteUpdateAsync(u => u.SetProperty(refreshToken => refreshToken.IsValid, false));
		}
		
		private Task MarkTokenAsInvalid(RefreshToken refreshToken)
		{
			refreshToken.IsValid = false;

			return _db.SaveChangesAsync();
		}

		
	}
}
