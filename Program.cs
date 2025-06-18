using AliveOnD_ID.Models;
using AliveOnD_ID.Models.Configurations;
using AliveOnD_ID.Services;
using AliveOnD_ID.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

var servicesSection = builder.Configuration.GetSection("Services");
var didSection = servicesSection.GetSection("DID");
var apiKeySetting = didSection["ApiKey"];

if (!string.IsNullOrEmpty(apiKeySetting))
{
     var envValue = Environment.GetEnvironmentVariable(apiKeySetting);
    if (!string.IsNullOrEmpty(envValue))
    {
        builder.Configuration["Services:DID:ApiKey"] = envValue;
    }
}

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add MVC Controllers for API testing
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
    
    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Configure Options
builder.Services.Configure<ServiceConfiguration>(
    builder.Configuration.GetSection("Services"));
builder.Services.Configure<RedisConfig>(
    builder.Configuration.GetSection("Redis"));
builder.Services.Configure<ChatConfig>(
    builder.Configuration.GetSection("Chat"));

// Add Memory Cache for in-memory storage
builder.Services.AddMemoryCache();

// Add HttpClient with custom configuration
builder.Services.AddHttpClient<AudioToTextService>(client =>
{
    // Configure default headers, timeout, etc.
    client.DefaultRequestHeaders.Add("User-Agent", "AliveOnD-ID/1.0");
});

builder.Services.AddHttpClient<LLMService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "AliveOnD-ID/1.0");
});

builder.Services.AddHttpClient<AvatarStreamService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "AliveOnD-ID/1.0");
});

// Register Services
builder.Services.AddScoped<IStorageService, InMemoryStorageService>();
builder.Services.AddScoped<IChatSessionService, ChatSessionService>();
builder.Services.AddScoped<IAudioProcessingService, AudioProcessingService>();
builder.Services.AddScoped<IAudioToTextService, AudioToTextService>();
builder.Services.AddScoped<ILLMService, LLMService>();
builder.Services.AddScoped<IAvatarStreamService, AvatarStreamService>();

// Add SignalR for real-time communication
builder.Services.AddSignalR();

// Add CORS if needed for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    // Enable Swagger in development
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AliveOnD-ID API v1");
        c.RoutePrefix = "swagger"; // Swagger UI at /swagger
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Serve audio files statically
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "AudioFiles")),
    RequestPath = "/audio"
});

app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseCors();
}

app.MapRazorPages();
app.MapBlazorHub();
app.MapControllers(); // Add API controllers
// Serve the avatar chat HTML page
app.MapGet("/avatar-chat", async (HttpContext context) =>
{
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath, "avatar-chat.html"));
});
app.MapFallbackToPage("/_Host");

// Add API endpoints for audio upload and other operations
app.MapPost("/api/audio/upload", async (
    HttpRequest request,
    IServiceProvider serviceProvider) =>
{
    try
    {
        var audioProcessingService = serviceProvider.GetRequiredService<IAudioProcessingService>();
        var chatConfig = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ChatConfig>>();
        
        var form = await request.ReadFormAsync();
        var audioFile = form.Files["audio"];
        var sessionId = form["sessionId"].ToString();

        if (audioFile == null || string.IsNullOrEmpty(sessionId))
        {
            return Results.BadRequest("Audio file and session ID are required");
        }

        using var stream = audioFile.OpenReadStream();
        var audioData = new byte[stream.Length];
        await stream.ReadAsync(audioData);

        // Validate audio
        var isValid = await audioProcessingService.ValidateAudioAsync(
            audioData, chatConfig.Value.MaxAudioDurationSeconds);
        
        if (!isValid)
        {
            return Results.BadRequest("Audio validation failed");
        }

        // Convert to MP3 if needed
        var mp3Data = await audioProcessingService.ConvertToMp3Async(
            audioData, Path.GetExtension(audioFile.FileName));

        // Save file
        var relativePath = await audioProcessingService.SaveAudioFileAsync(mp3Data, sessionId);

        return Results.Ok(new { audioUrl = $"/audio/{relativePath}" });
    }
    catch (Exception ex)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error uploading audio file");
        return Results.Problem("An error occurred while processing the audio file");
    }
});

// Add endpoint for manual audio cleanup
app.MapPost("/api/admin/cleanup-audio", async (
    IServiceProvider serviceProvider) =>
{
    try
    {
        var audioProcessingService = serviceProvider.GetRequiredService<IAudioProcessingService>();
        var deletedCount = await audioProcessingService.CleanupOldFilesAsync(TimeSpan.FromDays(7));
        return Results.Ok(new { deletedCount });
    }
    catch (Exception ex)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error during audio cleanup");
        return Results.Problem("An error occurred during cleanup");
    }
});

app.Run();