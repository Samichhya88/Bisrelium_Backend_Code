// ! // Importing necessary namespaces

using BisleriumBloggers.DTOs.Base;
using BisleriumBloggers.DTOs.Email;
using BisleriumBloggers.DTOs.Profile;
using BisleriumBloggers.Interfaces.Repositories.Base;
using BisleriumBloggers.Interfaces.Services;
using BisleriumBloggers.Models;
using BisleriumBloggers.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using System.Net;
using System.Reflection.Metadata;


// Defining the namespace and class for profile controller
namespace BisleriumBloggers.Controllers;

// Attributes for authorization, API controller, and route
[Authorize]
[ApiController]
[Route("api/profile")]
public class ProfileController : Controller
{
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private readonly IGenericRepository _genericRepository;

// Constructor injection for required services
    public ProfileController(IEmailService emailService, IGenericRepository genericRepository, IUserService userService)
    {
        _emailService = emailService;
        _genericRepository = genericRepository;
        _userService = userService;
    }
// GET method to fetch profile details
    [HttpGet("profile-details")]
    public IActionResult GetProfileDetails()
    {
        var userId = _userService.UserId;

        var user = _genericRepository.GetById<User>(userId);

        var role = _genericRepository.GetById<Role>(user.RoleId);

        var result = new ProfileDetailsDto()
        {
            UserId = user.Id,
            FullName = user.FullName,
            Username = user.UserName,
            EmailAddress = user.EmailAddress,
            RoleId = role.Id,
            RoleName = role.Name,
            ImageURL = user.ImageURL ?? "dummy.svg",
            MobileNumber = user.MobileNo ?? ""
        };
 // Returning profile details response
        return Ok(new ResponseDto<ProfileDetailsDto>()
        {
            Message = "Successfully Fetched",
            StatusCode = HttpStatusCode.OK,
            TotalCount = 1,
            Status = "Success",
            Result = result
        });
    }

// PATCH method to update profile details
    [HttpPatch("update-profile")]
    public IActionResult UpdateProfileDetails(ProfileDetailsDto profileDetails)
    {
        var user = _genericRepository.GetById<User>(profileDetails.UserId);

        user.FullName = profileDetails.FullName;
        user.MobileNo = profileDetails.MobileNumber;
        user.EmailAddress = profileDetails.EmailAddress;

        _genericRepository.Update(user);

 // Returning update profile response
        return Ok(new ResponseDto<object>()
        {
            Message = "Successfully Updated",
            StatusCode = HttpStatusCode.OK,
            TotalCount = 1,
            Status = "Success",
            Result = true
        });
    }

// DELETE method to delete user profile
    [HttpDelete("delete-profile")]
    public IActionResult DeleteProfile()
    {
        var userId = _userService.UserId;

        var user = _genericRepository.GetById<User>(userId);

        var blogs = _genericRepository.Get<Blog>(x => x.CreatedBy == user.Id);

        var blogImages = _genericRepository.Get<BlogImage>(x => blogs.Select(z => z.Id).Contains(x.BlogId));

        var comments = _genericRepository.Get<Comment>(x => x.CreatedBy == user.Id);

        var reactions = _genericRepository.Get<Reaction>(x => x.CreatedBy == user.Id);

        _genericRepository.RemoveMultipleEntity(reactions);

        _genericRepository.RemoveMultipleEntity(comments);

        _genericRepository.RemoveMultipleEntity(blogImages);

        _genericRepository.RemoveMultipleEntity(blogs);

        _genericRepository.Delete(user);

 // Returning delete profile response
        return Ok(new ResponseDto<object>()
        {
            Message = "Successfully Deleted",
            StatusCode = HttpStatusCode.OK,
            TotalCount = 1,
            Status = "Success",
            Result = true
        });
    }

// POST method to change user password
    [HttpPost("change-password")]
    public IActionResult ChangePassword(ChangePasswordDto changePassword)
    {
        var userId = _userService.UserId;

        var user = _genericRepository.GetById<User>(userId);

        var isValid = Password.VerifyHash(changePassword.CurrentPassword, user.Password);

        if (isValid)
        {
            user.Password = Password.HashSecret(changePassword.NewPassword);

            _genericRepository.Update(user);

 // Returning change password response
            return Ok(new ResponseDto<object>()
            {
                Message = "Successfully Updated",
                StatusCode = HttpStatusCode.OK,
                TotalCount = 1,
                Status = "Success",
                Result = true
            });
        }

// Returning invalid password response
        return BadRequest(new ResponseDto<object>()
        {
            Message = "Password not valid",
            StatusCode = HttpStatusCode.BadRequest,
            TotalCount = 1,
            Status = "Invalid",
            Result = false
        });
    }

 // POST method to reset user password
    [HttpPost("reset-password")]
    public IActionResult ResetPassword(string emailAddress)
    {
        var user = _genericRepository.GetFirstOrDefault<User>(x => x.EmailAddress == emailAddress);

        if (user == null)
        {
             // Returning user not found response
            return BadRequest(new ResponseDto<object>()
            {
                Message = "User not found",
                StatusCode = HttpStatusCode.BadRequest,
                TotalCount = 1,
                Status = "Invalid",
                Result = false
            });
        }

        const string newPassword = "Admin@123";

        var message =
            $"Dear {user.FullName}, <br><br> " +
            $"We have received a request to reset your password and it has been reset successfully. " +
            $"Your new allocated password is {newPassword}.<br><br>" +
            $"Regards,<br>" +
            $"Bislerium.";

        var email = new EmailDto()
        {
            Email = user.EmailAddress,
            Message = message,
            Subject = "Reset Password - Bislerium"
        };

        _emailService.SendEmail(email);

 // Returning reset password response
        return Ok(new ResponseDto<object>()
        {
            Message = "Successfully Updated",
            StatusCode = HttpStatusCode.OK,
            TotalCount = 1,
            Status = "Success",
            Result = true
        });
    }
}