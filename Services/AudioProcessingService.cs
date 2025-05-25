using Microsoft.Extensions.Options;
using AliveOnD_ID.Models;
using AliveOnD_ID.Services.Interfaces;

namespace AliveOnD_ID.Services;

public class AudioProcessingService : IAudioProcessingService
{
    private readonly ChatConfig _config;
    private readonly ILogger<AudioProcessingService> _logger;
    private readonly string _audioStoragePath;

    public AudioProcessingService(
        IOptions<ChatConfig> config,
        ILogger<AudioProcessingService> logger,
        IWebHostEnvironment environment)
    {
        _config = config.Value;
        _logger = logger;
        _audioStoragePath = Path.Combine(environment.ContentRootPath, "AudioFiles");
        
        // Ensure audio directory exists
        Directory.CreateDirectory(_audioStoragePath);
    }

    public async Task<byte[]> ConvertToMp3Async(byte[] audioData, string originalFormat)
    {
        try
        {
            _logger.LogDebug("Converting audio from {Format} to MP3, size: {Size} bytes", 
                originalFormat, audioData.Length);

            // For now, assume the input is already in a compatible format
            // You can extend this to handle different input formats using NAudio
            
            if (originalFormat.ToLower().Contains("mp3"))
            {
                _logger.LogDebug("Audio is already MP3, returning as-is");
                return audioData;
            }

            // Basic conversion logic - extend as needed
            using var inputStream = new MemoryStream(audioData);
            using var outputStream = new MemoryStream();
            
            // This is a simplified conversion - you may need more sophisticated
            // conversion depending on your input formats
            await inputStream.CopyToAsync(outputStream);
            
            var result = outputStream.ToArray();
            _logger.LogDebug("Audio conversion completed, output size: {Size} bytes", result.Length);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting audio from {Format} to MP3", originalFormat);
            throw;
        }
    }

    public async Task<bool> ValidateAudioAsync(byte[] audioData, int maxDurationSeconds)
    {
        try
        {
            if (audioData == null || audioData.Length == 0)
            {
                _logger.LogWarning("Audio data is empty");
                return false;
            }

            // Basic size validation (rough estimate)
            // Assuming average bitrate of 128 kbps for MP3
            var estimatedDurationSeconds = audioData.Length / (128 * 1024 / 8);
            
            if (estimatedDurationSeconds > maxDurationSeconds)
            {
                _logger.LogWarning("Audio duration ({EstimatedDuration}s) exceeds maximum ({MaxDuration}s)", 
                    estimatedDurationSeconds, maxDurationSeconds);
                return false;
            }

            // Additional validation can be added here
            // - Check file headers
            // - Validate audio format
            // - Check for corruption

            _logger.LogDebug("Audio validation passed, estimated duration: {Duration}s", 
                estimatedDurationSeconds);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating audio data");
            return false;
        }
    }

    public async Task<string> SaveAudioFileAsync(byte[] audioData, string sessionId)
    {
        try
        {
            // Create session-specific directory
            var sessionDirectory = Path.Combine(_audioStoragePath, sessionId);
            Directory.CreateDirectory(sessionDirectory);

            // Generate unique filename
            var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.mp3";
            var filePath = Path.Combine(sessionDirectory, fileName);

            // Save file
            await File.WriteAllBytesAsync(filePath, audioData);
            
            _logger.LogInformation("Saved audio file: {FilePath}, size: {Size} bytes", 
                filePath, audioData.Length);

            // Return relative path for URL generation (Windows-compatible)
            var relativePath = Path.Combine(sessionId, fileName)
                .Replace(Path.DirectorySeparatorChar, '/'); // Ensure forward slashes for URLs
            
            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving audio file for session {SessionId}", sessionId);
            throw;
        }
    }

    // Helper method to get full file path from relative path
    public string GetFullPath(string relativePath)
    {
        return Path.Combine(_audioStoragePath, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    // Helper method to clean up old audio files (can be called manually or scheduled)
    public async Task<int> CleanupOldFilesAsync(TimeSpan olderThan)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow - olderThan;
            var deletedCount = 0;

            var directories = Directory.GetDirectories(_audioStoragePath);
            
            foreach (var directory in directories)
            {
                var files = Directory.GetFiles(directory, "*.mp3");
                
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTimeUtc < cutoffDate)
                    {
                        File.Delete(file);
                        deletedCount++;
                        _logger.LogDebug("Deleted old audio file: {FilePath}", file);
                    }
                }

                // Remove empty directories
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                    _logger.LogDebug("Deleted empty directory: {DirectoryPath}", directory);
                }
            }

            _logger.LogInformation("Cleanup completed, deleted {Count} old audio files", deletedCount);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during audio file cleanup");
            return 0;
        }
    }
}