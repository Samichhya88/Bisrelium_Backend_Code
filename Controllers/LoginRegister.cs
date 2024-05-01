using Azure;
using BisleriumBloggers.Constants;
using BisleriumBloggers.DTOs.Account;
using BisleriumBloggers.DTOs.Base;
using BisleriumBloggers.Interfaces.Repositories.Base;
using BisleriumBloggers.Models;
using BisleriumBloggers.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace BisleriumBloggeres.Controllers
{
    // Controller for handling account-related operations
    [ApiController]
    [Route("api/account")]
    public class AccountController : Controller
    {
        private readonly IGenericRepository _genericRepository;
        private readonly JWTSettings _jwtSettings;

        // Constructor to initialize the controller with required services
        public AccountController(IGenericRepository genericRepository, IOptions<JWTSettings> jwtSettings)
        {
            _genericRepository = genericRepository;
            _jwtSettings = jwtSettings.Value;
        }

        // Method to authenticate users and generate JWT tokens
        [HttpPost("login")]
        public IActionResult Login(LoginDto loginRequest)
        {
            // Retrieving user details from the database
            var user = _genericRepository.GetFirstOrDefault<User>(x => x.EmailAddress == loginRequest.EmailAddress);

            // Handling scenario when user is not found
            if (user == null)
            {
                return NotFound(new ResponseDto<bool>()
                {
                    Message = "User not found",
                    Result = false,
                    Status = "Not Found",
                    StatusCode = HttpStatusCode.NotFound,
                    TotalCount = 0
                });
            }

            // Verifying the provided password
            var isPasswordValid = Password.VerifyHash(loginRequest.Password, user.Password);

            // Handling scenario when password is incorrect
            if (!isPasswordValid)
            {
                return Unauthorized(new ResponseDto<bool>()
                {
                    Message = "Password incorrect",
                    Result = false,
                    Status = "Unauthorized",
                    StatusCode = HttpStatusCode.Unauthorized,
                    TotalCount = 0
                });
            }

            // Retrieving user's role
            var role = _genericRepository.GetById<Role>(user.RoleId);

            // Generating JWT token claims
            var authClaims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, (user.Id.ToString() ?? null) ?? string.Empty),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.EmailAddress),
                new(ClaimTypes.Role, role.Name ?? ""),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            // Generating symmetric signing key
            var symmetricSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            
            // Generating signing credentials
            var signingCredentials = new SigningCredentials(symmetricSigningKey, SecurityAlgorithms.HmacSha256);

            // Setting expiration time for the token
            var expirationTime = DateTime.UtcNow.AddMinutes(Convert.ToInt32(_jwtSettings.DurationInMinutes));

            // Creating JWT token
            var accessToken = new JwtSecurityToken(
               _jwtSettings.Issuer,
               _jwtSettings.Audience,
               claims: authClaims,
               signingCredentials: signingCredentials,
               expires: expirationTime
            );

            // Creating user details DTO with token
            var userDetails = new UserDto()
            {
                Id = user.Id,
                Name = user.FullName,
                Username = user.UserName,
                EmailAddress = user.EmailAddress,
                RoleId = role.Id,
                Role = role.Name ?? "",
                ImageUrl = user.ImageURL ?? "dummy.svg",
                Token = new JwtSecurityTokenHandler().WriteToken(accessToken)
            };

            // Returning success response with user details and token
            return Ok(new ResponseDto<UserDto>()
            {
                Message = "Successfully authenticated",
                Result = userDetails,
                Status = "Success",
                StatusCode = HttpStatusCode.OK,
                TotalCount = 1
            });
        }

        // Method to register new users
        [HttpPost("register")]
        public IActionResult Register(RegisterDto register)
        {
            // Checking if user with same email or username already exists
            var existingUser = _genericRepository.GetFirstOrDefault<User>(x =>
               x.EmailAddress == register.EmailAddress || x.UserName == register.Username);

            // Handling scenario when user already exists
            if (existingUser == null)
            {
                // Retrieving default role for new user
                var role = _genericRepository.GetFirstOrDefault<Role>(x => x.Name == Constants.Roles.Blogger);

                // Creating new user object
                var appUser = new User()
                {
                    FullName = register.FullName,
                    EmailAddress = register.EmailAddress,
                    RoleId = role!.Id,
                    Password = Password.HashSecret(register.Password),
                    UserName = register.Username,
                    MobileNo = register.MobileNumber,
                    ImageURL = register.ImageURL
                };

                // Inserting new user into database
                _genericRepository.Insert(appUser);

                // Returning success response
                return Ok(new ResponseDto<object>()
                {
                    Message = "Successfully registered",
                    Result = true,
                    Status = "Success",
                    StatusCode = HttpStatusCode.OK,
                    TotalCount = 1
                });
            }

            // Returning bad request response if user already exists
            return BadRequest(new ResponseDto<bool>()
            {
                Message = "Existing user with the same user name or email address",
                Result = false,
                Status = "Bad Request",
                StatusCode = HttpStatusCode.BadRequest,
                TotalCount = 0
            });
        }
    }
}
