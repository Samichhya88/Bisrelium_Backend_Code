using Azure;
using BisleriumBloggers.DTOs.Base;
using BisleriumBloggers.DTOs.Blog;
using BisleriumBloggers.Interfaces.Repositories.Base;
using BisleriumBloggers.Interfaces.Services;
using BisleriumBloggers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Reflection.Metadata;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace BisleriumBloggers.Controllers;
[Authorize]
[ApiController]
[Route("api/blog")]
public class BlogController : Controller
{
    private readonly IUserService _userService;
    private readonly IGenericRepository _genericRepository;

    public BlogController(IGenericRepository genericRepository, IUserService userService)
    {
        _genericRepository = genericRepository;
        _userService = userService;
    }



    /*This method creates a new blog post, assigns it to the current user, and returns a success response upon insertion.*/

    [HttpPost("create-blog")]
    public IActionResult CreateBlog(BlogCreateDto blog)
    {
        var userId = _userService.UserId;

        var user = _genericRepository.GetById<User>(userId);

        var blogModel = new Blog()
        {
            Title = blog.Title,
            Body = blog.Body,
            Location = blog.Location,
            Reaction = blog.Reaction,
            BlogImages = blog.Images?.Select(x => new BlogImage()
            {
                ImageURL = x,
                IsActive = true,
                CreatedAt = DateTime.Now,
                CreatedBy = user.Id
            }).ToList(),
            CreatedAt = DateTime.Now,
            CreatedBy = user.Id,
        
        
        };


        _genericRepository.Insert(blogModel);

        return Ok(new ResponseDto<object>()
        {
            Status = "Success",
            StatusCode = HttpStatusCode.OK,
            TotalCount = 1,
            Message = "Successfully Inserted",
            Result = true
        });
    }


    /*This method updates a blog post, creates a log of the update, and returns a success response upon successful update*/
    [HttpPatch("update-blog")]
    public IActionResult UpdateBlog(BlogDetailsDto blog)
    {
        var userId = _userService.UserId;

        var user = _genericRepository.GetById<User>(userId);

        var blogModel = _genericRepository.GetById<Blog>(blog.Id);

        var blogLog = new BlogLog()
        {
            BlogId = blogModel.Id,
            Title = blogModel.Title,
            Location = blogModel.Location,
            Reaction = blogModel.Reaction,
            CreatedAt = DateTime.Now,
            CreatedBy = user.Id,
            Body = blogModel.Body,
            IsActive = false
        };

        _genericRepository.Insert(blogLog);

        blogModel.Title = blog.Title;
        blogModel.Body = blog.Body;
        blogModel.Location = blog.Location;
        blogModel.Reaction = blog.Reaction;

        blogModel.LastModifiedAt = DateTime.Now;
        blogModel.LastModifiedBy = user.Id;

        _genericRepository.Update(blogModel);

        return Ok(new ResponseDto<object>()
        {
            Status = "Success",
            StatusCode = HttpStatusCode.OK,
            TotalCount = 1,
            Message = "Successfully Updated",
            Result = true
        });
    }


    /*This method soft deletes a blog post, updating its status and recording deletion details,
    and returns a success response upon completion*/
    [HttpDelete("delete-blog/{blogId:int}")]
    public IActionResult DeletePost(int blogId)
    {
        var userId = _userService.UserId;

        var user = _genericRepository.GetById<User>(userId);

        var blogModel = _genericRepository.GetById<Blog>(blogId);

        blogModel.IsActive = false;
        blogModel.DeletedAt = DateTime.Now;
        blogModel.DeletedBy = user.Id;

        _genericRepository.Update(blogModel);

        return Ok(new ResponseDto<object>()
        {
            Status = "Success",
            StatusCode = HttpStatusCode.OK,
            TotalCount = 1,
            Message = "Successfully Updated",
            Result = true
        });
    }
}