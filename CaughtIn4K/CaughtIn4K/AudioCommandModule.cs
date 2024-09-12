using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.IO;
using System.Threading.Tasks;

// Manages bot commands related to audio recording
public class AudioCommandModule : ModuleBase<SocketCommandContext>
{
    private readonly AudioService _audioService;

    public AudioCommandModule(AudioService audioService)
    {
        _audioService = audioService;
    }

    // Command to join the voice channel
    [Command("join")]
    [RequireAdmin]
    public async Task JoinAsync()
    {
        var user = Context.User as IGuildUser;
        var voiceChannel = user?.VoiceChannel;

        if (voiceChannel == null)
        {
            await ReplyAsync("You need to be in a voice channel for me to join.");
            return;
        }

        await _audioService.JoinVoiceChannelAsync(voiceChannel);
        await ReplyAsync($"Joined {voiceChannel.Name}.");
    }

    // Command to capture the last 15 seconds of audio
    [Command("capture")]
    [RequireAdmin]
    public async Task CaptureAsync(int duration = 15)
    {
        var user = Context.User as IGuildUser;
        var voiceChannel = user?.VoiceChannel;

        // Ensure user is in a voice channel
        if (voiceChannel == null)
        {
            await ReplyAsync("You need to be in a voice channel to capture audio.");
            return;
        }

        // Clamp the requested duration to the maximum buffer duration
        int maxDuration = _audioService.MaxBufferDuration;
        duration = Math.Min(duration, maxDuration);

        string filePath = Path.Combine("temp", $"{Context.User.Id}.mp4");

        try
        {
            // Save buffered audio data to the file and upload it
            await _audioService.SaveBufferedAudioToFile(voiceChannel.Id, filePath, duration); // Pass duration and channel ID
            await ReplyAsync("Uploading audio...");
            await _audioService.UploadAudioClipAsync(Context.Channel, filePath);
        }
        catch (Exception ex)
        {
            await ReplyAsync($"An error occurred: {ex.Message}");
        }
        finally
        {
            // Ensure the temporary file is deleted
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}