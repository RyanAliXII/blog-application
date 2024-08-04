using System.Configuration;
using System.Text.Json;
using System.Text.Json.Nodes;
using BlogApplication.Areas.App.Models;
using BlogApplication.Areas.App.ViewModels;
using BlogApplication.Data;
using BlogApplication.Repository;
using BlogApplication.Services.EditorImageUrlExtractor;
using BlogApplication.Services.FileStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BlogApplication.Extensions;
using MimeTypes;

namespace BlogApplication.Areas.App.Controllers
{
    [Authorize]
    [Area("App")]
    public class PostController : Controller
    {
        protected readonly ILogger<PostController> _logger;
        protected readonly IFileStorage _fileStorage;
        protected readonly IConfiguration _config;
        protected readonly IEditorImageUrlExtractor _imageUrlExtractor;
        protected readonly PostRepository _postRepository; 
        protected readonly UserManager<User> _userManager;
        public PostController(ILogger<PostController> 
            logger, IFileStorage fileStorage, 
            IConfiguration config, 
            IEditorImageUrlExtractor imageUrlExtractor, 
            PostRepository postRepository,
            UserManager<User> userManager
        )
        {
            _logger = logger;
            _fileStorage = fileStorage;
            _config = config;
            _imageUrlExtractor = imageUrlExtractor ;
            _postRepository = postRepository;
            _userManager = userManager;
        }
        public IActionResult Create()
        {
            var config = _config.GetSection("AWS");
            var serviceUrl = config["ServiceUrl"] ?? "";
            var bucket = config["Bucket"] ?? "";
            ViewData["S3Url"] =  $"{serviceUrl}/{bucket}";
            return View();
        }
        
        public async Task<IActionResult> Edit(Guid Id)
        {
            try{
                if(Id == Guid.Empty){
                    return NotFound();
                }
                var post =  await _postRepository.GetByIdAsync(Id);
                if(post == null){
                    return NotFound();
                }
                
                var config = _config.GetSection("AWS");
                var serviceUrl = config["ServiceUrl"] ?? "";
                var bucket = config["Bucket"] ?? "";
                ViewData["S3Url"] =  $"{serviceUrl}/{bucket}";
                return View(post); 
            }
            catch(Exception ex){
                _logger.LogInformation(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "Unknown error occured.");
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ActionResult> Create([FromBody]CreatePost post){
            try
            {
         
                if (!ModelState.IsValid){
                    return BadRequest(new {
                     message = "Validation error",
                     errors = ModelState.ToValidationErrors()
                    });
                }
                var config = _config.GetSection("AWS");
                var bucket = config["Bucket"] ?? throw new ConfigurationErrorsException("Bucket missing from configuration");
                if(!string.IsNullOrEmpty(post?.Thumbnail)){
                    var newThumbnailKey = post.Thumbnail.Replace("temp", "contents");
                    await _fileStorage.CopyFile(bucket, post.Thumbnail, newThumbnailKey);
                    post.Thumbnail = newThumbnailKey;
                }
                var baseUrl = $"{config["ServiceUrl"]}/{bucket}";

                /*
                    Extract image and replace their source from editor.
                    Initially, when uploading an image, the image is stored in temporary folder named "temp" in a bucket.
                    Upon submission, the images are then moved to another folder named "contents". 
                    This is important because there is lifecycle rule in a bucket that deletes files in temp folder.

                */    
                foreach(var block in _imageUrlExtractor.ExtractWithBaseUrl(post?.Content ?? [], baseUrl )){
                   await EditImageSourceAndMoveToAnotherLocation(block, bucket);
                }
        
                var user = await _userManager.GetUserAsync(User);
                if(user == null){
                    return StatusCode(StatusCodes.Status401Unauthorized, new {
                        message = "Unauthorized"
                    });
                }

                var jsonContent = JsonSerializer.Serialize(post?.Content ?? []);
        
                await _postRepository.CreateAsync(new Post{
                    Content = jsonContent,
                    Thumbnail = post?.Thumbnail,
                    Title = post?.Title ?? "",
                    UserId = user.Id,
                });
                
                await _postRepository.SaveAsync();
                return Ok(new {
                    message = "Post created "  
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("{ex}", ex);
                 return StatusCode(StatusCodes.Status500InternalServerError, new {
                    status = StatusCodes.Status500InternalServerError,
                    message = "Unknown error occurred."
                });
            }

        }
        [HttpPut]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "PostOwnerPolicy")]
        public async Task<IActionResult> Edit([FromBody]EditPost post, Guid id)
        {
             try
            {
            
                if(id == Guid.Empty){
                    return BadRequest(new {
                        message = "Invalid Id",
                        status = StatusCodes.Status400BadRequest,
                    });
                }
                if(!ModelState.IsValid){
                    return BadRequest(new {
                     message = "Validation error",
                     errors = ModelState.ToValidationErrors()
                    });
                }

                var dbPost = await _postRepository.GetByIdAsync(id);
                
                if(dbPost is null){
                     return NotFound(new {
                        message = "Not Found",
                        status = StatusCodes.Status404NotFound,
                    });
                }
               
                var config = _config.GetSection("AWS");
                var bucket = config["Bucket"] ?? throw new ConfigurationErrorsException("Bucket missing from configuration");
                if(!string.IsNullOrEmpty(post.Thumbnail)){
                    var newThumbnailKey = post.Thumbnail.Replace("temp", "contents");
                    await _fileStorage.CopyFile(bucket, post.Thumbnail, newThumbnailKey);
                    if(dbPost.Thumbnail is not null && dbPost.Thumbnail.Length > 0){
                        await _fileStorage.DeleteFile(bucket, dbPost.Thumbnail);
                    }
                    dbPost.Thumbnail = newThumbnailKey;
                }

              
                var baseUrl = $"{config["ServiceUrl"]}/{bucket}";
                /*
                    Assumes all uploaded images are new 
                */
                var newImageUrls = _imageUrlExtractor.ExtractWithBaseUrl(post.Content, baseUrl).Aggregate(new Dictionary<string, string>(), (a, block)=>{
                    var fileNode = GetImageFileNodeFromBlockDataObject(block.Data);
                    if (fileNode is null) return a;
                    var urlNode = fileNode["url"] as JsonValue;
                    if (urlNode is null || urlNode.TryGetValue<string>(out var currentUrl) == false) return a;
                    a[currentUrl] = currentUrl;
                    return a; 
                });
                var dbPostContent = JsonSerializer.Deserialize<List<EditorJSDataBlock>>(dbPost.Content);
                if(dbPostContent is null){
                    _logger.LogError("Post content is empty {Id}", dbPost.Id);
                    return StatusCode(StatusCodes.Status500InternalServerError, new {
                            message = "Unknown error occured",
                            status = StatusCodes.Status500InternalServerError,
                    });
                }
                const int FolderNameSegmentIndex = 2;


                foreach(var block in post.Content){
                   if(block.Type == "image"){
                        var fileNode = GetImageFileNodeFromBlockDataObject(block.Data);
                        if(fileNode is null) continue;
                        var urlString = ExtractUrlFromFileNode(fileNode);
                        if (urlString is null) continue;
                        var uri = new Uri(urlString);
                        if(uri.Segments.Length < FolderNameSegmentIndex) continue;
                        var folder =  uri.Segments[FolderNameSegmentIndex];
                        if(folder == "temp/"){
                            await EditImageSourceAndMoveToAnotherLocation(block, bucket);
                        }                    
                    } 
                }

                foreach(var block in dbPostContent){
                     if(block.Type == "image") {
                        var fileNode = GetImageFileNodeFromBlockDataObject(block.Data);
                        if(fileNode is null) continue;
                        var urlString = ExtractUrlFromFileNode(fileNode);
                        if (urlString is null) continue;
                        if(!newImageUrls.ContainsKey(urlString)){
                            var uri = new Uri(urlString);
                            if(uri.Segments.Length < FolderNameSegmentIndex) continue;
                            var folder =  uri.Segments[FolderNameSegmentIndex]; 
                            var filename = uri.Segments.Last();
                            var key = $"{folder}{filename}";
                            await _fileStorage.DeleteFile(bucket, key);
                        }
                      
                    } 
                }
            
                var jsonContent = JsonSerializer.Serialize(post?.Content ?? []);
                dbPost.Content = jsonContent;
                dbPost.Title = post?.Title ?? "";
                await _postRepository.SaveAsync();
                return Ok(new {
                    message = "Post updated",  
                });
            }
            catch (Exception ex)
            {
                
                _logger.LogError(ex.Message);
                 return StatusCode(StatusCodes.Status500InternalServerError, new {
                    status = StatusCodes.Status500InternalServerError,
                    message = "Unknown error occurred."
                });
            }
        }
        [HttpDelete]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "PostOwnerPolicy")]
        public async Task <IActionResult>Delete(Guid id){
            try{
                if(id == Guid.Empty){
                     return BadRequest(new {
                        message = "Invalid Id",
                         status = StatusCodes.Status400BadRequest,
                    });
                }

                var dbPost = await _postRepository.GetByIdAsync(id);
                
                if(dbPost is null){
                     return NotFound(new {
                        message = "Not Found",
                        status = StatusCodes.Status404NotFound,
                    });
                }
            
                await _postRepository.DeleteAsync(id);
                
                var config = _config.GetSection("AWS");
                var bucket = config["Bucket"] ?? throw new ConfigurationErrorsException("Bucket missing from configuration");
                var dbPostContent = JsonSerializer.Deserialize<List<EditorJSDataBlock>>(dbPost.Content);
                
                if(dbPostContent is null){
                    _logger.LogError("Post content is empty {Id}", dbPost.Id);
                    return StatusCode(StatusCodes.Status500InternalServerError, new {
                            message = "Unknown error occured",
                            status = StatusCodes.Status500InternalServerError,
                    });
                }
                const int FolderNameSegmentIndex = 2;
                foreach(var block in dbPostContent){
                    if(block.Type == "image"){
                        var fileNode = GetImageFileNodeFromBlockDataObject(block.Data);
                        if(fileNode is null) continue;
                        var urlString = ExtractUrlFromFileNode(fileNode);
                        if (urlString is null) continue;
                        var uri = new Uri(urlString);
                        if(uri.Segments.Length < FolderNameSegmentIndex) continue;
                        var folder =  uri.Segments[FolderNameSegmentIndex];
                        var filename = uri.Segments.LastOrDefault();
                        var key = $"{folder}{filename}";
                        await _fileStorage.DeleteFile(bucket, key);
                    } 
                }
                if(dbPost.Thumbnail is not null){
                      if(dbPost.Thumbnail.Length > 0){
                        await _fileStorage.DeleteFile(bucket, dbPost.Thumbnail);
                      }
                }
                await _postRepository.SaveAsync();
                return Ok(new {
                    message = "Post deleted",
                    status = StatusCodes.Status200OK,
                });


            }
            catch(Exception ex){
                _logger.LogError("{Message} {Trace}", ex.Message, ex.StackTrace);
                return StatusCode(StatusCodes.Status500InternalServerError, new {
                    status = StatusCodes.Status500InternalServerError,
                    message = "Unknown error occurred."
                });
            }
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            try{
                using var fileStream = file.OpenReadStream();
                var config = _config.GetSection("AWS");
                var bucket = config["Bucket"] ?? throw new ConfigurationErrorsException("Bucket missing from configuration");
                var filename = Guid.NewGuid().ToString();
                var extension = MimeTypeMap.GetExtension(file.ContentType);
                var key = $"temp/{filename}{extension}";
                var response = await _fileStorage.NewUploader(fileStream, key, bucket)
                .SetContentType(file.ContentType).UploadAsync();
                return Ok(new
                {
                    location = response.Key,
                }); 
            }
            catch(Exception ex){
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new {
                    status = StatusCodes.Status500InternalServerError,
                    message = "Unknown error occurred."
                });
            }
           
        }
       

        private JsonNode? GetImageFileNodeFromBlockDataObject(JsonObject? obj){
             var fileNode = obj?["file"];
             return fileNode;
        }

         private string? ExtractUrlFromFileNode(JsonNode? fileNode){
             var urlNode = fileNode?["url"] as JsonValue;
             return urlNode?.GetValue<string>();
        }
        private async Task EditImageSourceAndMoveToAnotherLocation(EditorJSDataBlock block, string bucket){
            

            const int FolderNameSegmentIndex = 2;
            var fileNode = GetImageFileNodeFromBlockDataObject(block.Data);
            if(fileNode is null) return;
            var urlString = ExtractUrlFromFileNode(fileNode);
            if (urlString is null) return;

            var uri = new Uri(urlString);
            
            /*
                The last segment of the url is the filename and the folder name is at 2nd Index.
                Feel free to change this, if folder structure is changed.
                For example: http://aws.amazon.com/sample-bucket/temp/robot.png
                Segment[0] = "/"
                Segment[1] = "sample-bucket/"
                Segment[2] = "temp/"   This is the folder name
                Segment[3] = "robot.png" This is the filename
            */
           
            if(uri.Segments.Length < FolderNameSegmentIndex) return;
  
            var lastSegment = uri.Segments.LastOrDefault();
            if (string.IsNullOrEmpty(lastSegment)) return;
            
            var segments = uri.Segments.ToList();
            segments[FolderNameSegmentIndex] = "contents/"; // Update the folder name

            // Reconstruct the new URI
            var host = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
            var newUriString = $"{uri.Scheme}://{host}{string.Join("", segments)}";
          
            var currentLocation = $"temp/{lastSegment}";
            var newLocation = $"contents/{lastSegment}";
            await _fileStorage.CopyFile(bucket, currentLocation, newLocation);
             _logger.LogInformation(newUriString);
            // Assign the new URL
            fileNode["url"] = JsonValue.Create(newUriString);
            
        }
    }
}