using Microsoft.EntityFrameworkCore;
using BlogApplication.Data;
using Amazon.S3;
using BlogApplication.Services.FileStorage.S3;
using BlogApplication.Services.FileStorage;
using BlogApplication.Repository;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using BlogApplication.Services.EditorImageUrlExtractor;
using BlogApplication.Policies.Requirements;
using Microsoft.AspNetCore.Authorization;
var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'ApplicationDbContextConnection' not found.");
// Add services to the container.
builder.Services.AddDbContext<ApplicationIdentityDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddSingleton(sp => S3.CreateClient(builder.Configuration));
builder.Services.AddScoped<IFileStorage>(sp => new S3FileStorage(sp.GetRequiredService<IAmazonS3>()));
builder.Services.AddScoped<IEditorImageUrlExtractor, EditorJSImageUrlExtractor>();
builder.Services.AddScoped<PostRepository, PostRepository>();
builder.Services.AddRouting(options=> options.LowercaseUrls = true);
builder.Services.AddIdentity<User, Role>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequiredLength = 10;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredUniqueChars = 1;


}).AddEntityFrameworkStores<ApplicationIdentityDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/App/Login";
});
builder.Services.AddControllersWithViews(options =>
{
    options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;

});

builder.Services.AddAuthorization(options=>{
    options.AddPolicy("PostOwnerPolicy", policy => policy.Requirements.Add(new PostOwnerRequirement()));
});
builder.Services.AddScoped<IAuthorizationHandler, PostOwnerHandler>();

var app = builder.Build();
S3.InitializeBucket(app.Services.GetRequiredService<IAmazonS3>(), app.Configuration);
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.MapAreaControllerRoute(name: "AppArea", areaName: "App", pattern: "App/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
