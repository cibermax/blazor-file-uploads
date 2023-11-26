using Azure.Storage.Blobs;
using FileUpload.Client.Pages;
using FileUpload.Components;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7155") });

//builder.Services.AddAzureClients(clientBuilder =>
//{
//    // Register clients for each service
//    clientBuilder.AddBlobServiceClient("your-connection-string");
//});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseAntiforgery();
app.UseHttpsRedirection();

app.UseStaticFiles();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(WasmUpload).Assembly);

app.MapPost("/upload", async ([FromForm] TestTicket ticket,
    [FromServices] IWebHostEnvironment env,
    [FromServices] BlobServiceClient blobClient) =>
{
    if (ticket.Attachment != null)
    {
        // Save locally
        string safeFileName = WebUtility.HtmlEncode(ticket.Attachment.FileName);
        var path = Path.Combine(env.ContentRootPath, "images", safeFileName);
        await using FileStream fs = new(path, FileMode.Create);
        await ticket.Attachment.CopyToAsync(fs);

        // Upload file to blob storage
        var rand = new Random().Next(10000);
        var docsContainer = blobClient.GetBlobContainerClient("tickets");
        await docsContainer.UploadBlobAsync(
            $"{rand}_{ticket.Attachment.FileName}",
            ticket.Attachment.OpenReadStream());
    }
    
    // TODO: Save title, description, image reference to a database
});

app.Run();

class TestTicket
{
    public string Title { get; set; }
    public string Description { get; set; }
    public IFormFile Attachment { get; set; }
}