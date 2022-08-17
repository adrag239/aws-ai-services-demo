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
    var comprehendClient = new AmazonComprehendClient(Amazon.RegionEndpoint.EUWest1);

    return comprehendClient;
});
builder.Services.AddScoped<IAmazonTranslate>(s =>
{
    var translateClient = new AmazonTranslateClient(Amazon.RegionEndpoint.EUWest1);

    return translateClient;
});
builder.Services.AddScoped<IAmazonTextract>(s =>
{
    var textractClient = new AmazonTextractClient(Amazon.RegionEndpoint.EUWest1);

    return textractClient;
});
builder.Services.AddScoped<IAmazonRekognition>(s =>
{
    var rekognitionClient = new AmazonRekognitionClient(Amazon.RegionEndpoint.EUWest1);

    return rekognitionClient;
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
