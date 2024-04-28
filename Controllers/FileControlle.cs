// ! Importing necessary namespaces

using BisleriumBloggers.DTOs.Base;
using BisleriumBloggers.DTOs.Upload;
using BisleriumBloggers.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using System.Net;


// Defining the namespace and class for file upload controller
namespace BisleriumBloggers.Controllers;

// Attributes for API controller and route
[ApiController]
[Route("api/file-upload")]
public class FileUploadController : Controller
{
    private readonly IFileUploadService _fileUploadService;

// Constructor injection for file upload service
    public FileUploadController(IFileUploadService fileUploadService)
    {
        _fileUploadService = fileUploadService;
    }

    // POST method for uploading files
    [HttpPost]
    public IActionResult UploadFile([FromForm] UploadDto uploads)
    {
         // Checking if the file path is a valid integer
        if (!int.TryParse(uploads.FilePath, out int filePathIndex))
        {
            // Returning a bad request response with error message
            return BadRequest(new ResponseDto<object>()
            {
                Message = "Invalid File Path.",
                StatusCode = HttpStatusCode.BadRequest,
                TotalCount = 0,
                Status = "Bad Request",
                Result = false
            });
        }

// Determining file paths based on index
        var filePaths = filePathIndex switch
        {
            1 => Constants.Constants.FilePath.UsersImagesFilePath,
            2 => Constants.Constants.FilePath.BlogsImagesFilePath,
            _ => ""
        };

// Checking if file paths are empty or null
        if (string.IsNullOrEmpty(filePaths))
        {
            // Returning a bad request response with error message
            return BadRequest(new ResponseDto<object>()
            {
                Message = "Invalid File Path.",
                StatusCode = HttpStatusCode.BadRequest,
                TotalCount = 0,
                Status = "Bad Request",
                Result = false
            });
        }

// Checking if file size is greater than 3MB
        const long maxSize = 3 * 1024 * 1024;

        if (uploads.Files.Any(upload => upload.Length > maxSize))
        {
            // Returning a bad request response with error message
            return BadRequest(new ResponseDto<object>()
            {
                Message = "Invalid File Size.",
                StatusCode = HttpStatusCode.BadRequest,
                TotalCount = 0,
                Status = "Bad Request",
                Result = false
            });
        }
// Uploading files and getting file names
        var fileNames = uploads.Files.Select(file => _fileUploadService.UploadDocument(filePaths, file)).ToList();

// Creating a response with uploaded file names
        var response = new ResponseDto<List<string>>()
        {
            Message = "File successfully uploaded.",
            Result = fileNames,
            StatusCode = HttpStatusCode.OK,
            Status = "Success",
            TotalCount = fileNames.Count
        };
  // Returning success response with uploaded file names
        return Ok(response);
    }
}
