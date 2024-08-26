using System.Configuration;
using System.Text.Json;
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
using BlogApplication.Helpers;


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
        protected readonly string _s3BucketName; 
        protected readonly string _s3ServiceUrl;
        protected readonly string _s3Url;
        protected readonly string _temporaryFolderName = "temp";
        protected readonly string _permanentFolderName = "contents";
        protected const int FolderNameSegmentIndex = 2;
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
            var s3Config = _config.GetSection("AWS");
            _s3BucketName = s3Config["Bucket"] ?? throw new ConfigurationErrorsException("Bucket is missing from configuration");
            _s3ServiceUrl = s3Config["ServiceUrl"] ?? throw new ConfigurationErrorsException("ServiceUrl missing from configuration");
            _s3Url = $"{_s3ServiceUrl}/{_s3BucketName}";
        }
        public IActionResult Create()
        {
            ViewData["S3Url"] =  _s3Url;
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
                ViewData["S3Url"] =  _s3Url;
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
               
                if(!string.IsNullOrEmpty(post?.Thumbnail)){
                    var newThumbnailKey = post.Thumbnail.Replace(_temporaryFolderName, _permanentFolderName);
                    await _fileStorage.CopyFile(_s3BucketName, post.Thumbnail, newThumbnailKey);
                    post.Thumbnail = newThumbnailKey;
                }
                  
                foreach(var block in _imageUrlExtractor.ExtractWithBaseUrl(post?.Content ?? [], _s3Url)){
                /*
                    Extract image and replace their source from editor.
                    Initially, when uploading an image, the image is stored in temporary folder.
                    Upon submission, the images are then moved to permanent one. 
                    This is important because there is lifecycle rule in a bucket that deletes files in temporary folder.

                */ 
                   var result = UpdateImageSourceFromTemporaryToPermanent(block);
                   if(result is null) continue;
                   await  _fileStorage.CopyFile(_s3BucketName, result.OldKey, result.NewKey);
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
                _logger.LogError("{Exception} {Trace}", ex.Message, ex.StackTrace);
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
               
                if(!string.IsNullOrEmpty(post.Thumbnail)){
                    var newThumbnailKey = post.Thumbnail.Replace(_temporaryFolderName, _permanentFolderName);
                    await _fileStorage.CopyFile(_s3BucketName, post.Thumbnail, newThumbnailKey);
                    if(!string.IsNullOrEmpty(dbPost.Thumbnail)){
                        await _fileStorage.DeleteFile(_s3BucketName, dbPost.Thumbnail);
                    }
                    dbPost.Thumbnail = newThumbnailKey;
                }
                /*
                    Create a dictionary of image with url as key. This will be use for checking if images still exists in updated content.
                */
                var newImageUrls = _imageUrlExtractor.ExtractWithBaseUrl(post.Content, _s3Url).Aggregate(new Dictionary<string, string>(), (a, block)=>{
                    var source = EditorImageBlockSource.GetImageSource(block.Data);
                    if(source.Value is null) return a;
                    a[source.Value] = source.Value;
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
                /*
                   Get images using the base URL: S3 URL + temporary folder name. Then upload and update the image source.
                 */
                foreach (var block in _imageUrlExtractor.ExtractWithBaseUrl(post.Content, $"{_s3Url}/{_temporaryFolderName}/")){
                    var source = EditorImageBlockSource.GetImageSource(block.Data);
                    if(source.Value is null) continue;
                    var result = UpdateImageSourceFromTemporaryToPermanent(block);
                    if(result is null) continue;
                    await _fileStorage.CopyFile(_s3BucketName, result.OldKey, result.NewKey);
                }

                /*
                  Check old images still existed in the content, if not delete the image.
                */
                foreach(var block in dbPostContent){
                     if(block.Type == "image") {
                        var source = EditorImageBlockSource.GetImageSource(block.Data);
                        if(source.Value is null) continue;
                        if(!newImageUrls.ContainsKey(source.Value)){
                            var uri = new Uri(source.Value);
                            if(uri.Segments.Length < FolderNameSegmentIndex) continue;
                            var folder =  uri.Segments[FolderNameSegmentIndex]; 
                            var filename = uri.Segments.Last();
                            var key = $"{folder}{filename}";
                            await _fileStorage.DeleteFile(_s3BucketName, key);
                        }
                      
                    } 
                }
            
                var jsonContent = JsonSerializer.Serialize(post?.Content ?? []);
                dbPost.Content = jsonContent;
                dbPost.Title = post?.Title ?? string.Empty;
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
                
                var dbPostContent = JsonSerializer.Deserialize<List<EditorJSDataBlock>>(dbPost.Content);
                
                if(dbPostContent is null){
                    _logger.LogError("Post content is empty {Id}", dbPost.Id);
                    return StatusCode(StatusCodes.Status500InternalServerError, new {
                            message = "Unknown error occured",
                            status = StatusCodes.Status500InternalServerError,
                    });
                }
                foreach(var block in dbPostContent){
                    if(block.Type == "image"){
                        var source = EditorImageBlockSource.GetImageSource(block.Data);
                        if (source.Value is null) continue;
                        var uri = new Uri(source.Value);
                        if(uri.Segments.Length < FolderNameSegmentIndex) continue;
                        var folder =  uri.Segments[FolderNameSegmentIndex];
                        var filename = uri.Segments.LastOrDefault();
                        var key = $"{folder}{filename}";
                        await _fileStorage.DeleteFile(_s3BucketName, key);
                    } 
                }
                if(dbPost.Thumbnail is not null){
                      if(dbPost.Thumbnail.Length > 0){
                        await _fileStorage.DeleteFile(_s3BucketName, dbPost.Thumbnail);
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
                var filename = Guid.NewGuid().ToString();
                var extension = MimeTypeMap.GetExtension(file.ContentType);
                var key = $"{_temporaryFolderName}/{filename}{extension}";
                var response = await _fileStorage.NewUploader(fileStream, key, _s3BucketName)
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
       
        private UpdateImageSourceResult? UpdateImageSourceFromTemporaryToPermanent(EditorJSDataBlock block){
            var imageSrc = EditorImageBlockSource.GetImageSource(block.Data);
            if(imageSrc.Value is null) return null;
            var uri = new Uri(imageSrc.Value);
            if(uri.Segments.Length < FolderNameSegmentIndex) return null;
  
            var lastSegment = uri.Segments.LastOrDefault();
            if (string.IsNullOrEmpty(lastSegment)) return null;
            
            var segments = uri.Segments.ToList();
            segments[FolderNameSegmentIndex] = $"{_permanentFolderName}/"; // Update the folder name

            // Reconstruct the new URI
            var host = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
            var newUrl = $"{uri.Scheme}://{host}{string.Join("", segments)}";
            
            var oldLocation =  $"{_temporaryFolderName}/{lastSegment}";
            var newLocation = $"{_permanentFolderName}/{lastSegment}";
            imageSrc.UpdateSource(newUrl);
            return new UpdateImageSourceResult{
                NewKey = newLocation,
                NewUrl = newUrl,
                OldKey = oldLocation,
            };
        }
    
        
    }
    record UpdateImageSourceResult{
        public string NewUrl = string.Empty;
        public string NewKey = string.Empty;
        public string OldKey = string.Empty;
    }
  
}
