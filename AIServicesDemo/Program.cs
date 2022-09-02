using Amazon.Comprehend;
using Amazon.Rekognition;
using Amazon.Textract;
using Amazon.Translate;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Add AI Services
builder.Services.AddScoped<IAmazonComprehend>(s =>
{
    return new AmazonComprehendClient(Amazon.RegionEndpoint.EUWest1);
});

builder.Services.AddScoped<IAmazonTranslate>(s =>
{
    return new AmazonTranslateClient(Amazon.RegionEndpoint.EUWest1);
});

builder.Services.AddScoped<IAmazonTextract>(s =>
{
    return new AmazonTextractClient(Amazon.RegionEndpoint.EUWest1);
});

builder.Services.AddScoped<IAmazonRekognition>(s =>
{
    return new AmazonRekognitionClient(Amazon.RegionEndpoint.EUWest1);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
