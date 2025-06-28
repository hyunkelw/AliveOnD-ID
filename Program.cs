using AliveOnD_ID.Models;
using AliveOnD_ID.Models.Configurations;
using AliveOnD_ID.Services;
using AliveOnD_ID.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

var servicesSection = builder.Configuration.GetSection("Services");
var didSection = servicesSection.GetSection("DID");
var apiKeySetting = didSection["ApiKey"];

// Substitute the D-ID API key setting with the value in the environment variable if available
if (!string.IsNullOrEmpty(apiKeySetting))
{
    var d_IdKeyValue = Environment.GetEnvironmentVariable(apiKeySetting);
    if (!string.IsNullOrEmpty(d_IdKeyValue))
    {
        builder.Configuration["Services:DID:ApiKey"] = d_IdKeyValue;
    }
}

// Substitute the AZURE SPEECH API key setting with the value in the environment variable if available
var azureSection = servicesSection.GetSection("AzureSpeechServices");
if (!string.IsNullOrEmpty(azureSection["Key"]))
{
    var speechKey = azureSection["Key"];
    var speechKeyValue = Environment.GetEnvironmentVariable(speechKey);
    if (!string.IsNullOrEmpty(speechKeyValue))
    {
        builder.Configuration["Services:AzureSpeechServices:Key"] = speechKeyValue;
    }
}

// Add MVC Controllers for API endpoints
builder.Services.AddControllers();

// Add Swagger/OpenAPI for testing
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "AliveOnD-ID API", 
        Version = "v1",
        Description = "API for testing AliveOnD-ID services"
    });
});

// Configure services
builder.Services.Configure<DIDConfig>(didSection);
builder.Services.Configure<AudioToTextConfig>(servicesSection.GetSection("AzureSpeechServices"));
builder.Services.Configure<ServiceConfiguration>(servicesSection);

// Add application services
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IAvatarStreamService, AvatarStreamService>();
builder.Services.AddScoped<IAudioToTextService, AudioToTextService>();
builder.Services.AddScoped<IAudioProcessingService, AudioProcessingService>();
builder.Services.AddScoped<ILLMService, LLMService>();
builder.Services.AddScoped<IChatSessionService, ChatSessionService>();
builder.Services.AddScoped<IStorageService, InMemoryStorageService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

// Map the default route to the HTML file
app.MapFallbackToFile("index.html");

app.Run();