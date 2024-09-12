using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.WebSocket;

public class AudioService
{
    private readonly DiscordSocketClient _client;
    private readonly ConcurrentDictionary<ulong, (IAudioClient audioClient, AudioBuffer audioBuffer, Task recordingTask, CancellationTokenSource cancellationTokenSource)> _voiceChannels;
    private readonly int _bufferSize;

    public AudioService(DiscordSocketClient client)
    {
        _client = client;
        _bufferSize = 15 * 48000 * 2;  // 15 seconds at 48kHz, 2 bytes per sample
        _voiceChannels = new ConcurrentDictionary<ulong, (IAudioClient, AudioBuffer, Task, CancellationTokenSource)>();
    }
    public int MaxBufferDuration => _bufferSize / 48000 / 2;

    public async Task JoinVoiceChannelAsync(IVoiceChannel channel)
    {
        if (_voiceChannels.ContainsKey(channel.Id))
            return;

        try
        {
            var audioClient = await channel.ConnectAsync();
            var audioBuffer = new AudioBuffer(_bufferSize);
            var cancellationTokenSource = new CancellationTokenSource();

            var recordingTask = Task.Run(() => RecordAudioContinuously(channel.Id, audioClient, audioBuffer, cancellationTokenSource.Token));

            _voiceChannels[channel.Id] = (audioClient, audioBuffer, recordingTask, cancellationTokenSource);
        }
        catch (TimeoutException ex)
        {
            Console.WriteLine($"[ERROR] Timeout while attempting to join voice channel {channel.Id}: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Unexpected error while attempting to join voice channel {channel.Id}: {ex.Message}");
        }
    }

    public async Task LeaveVoiceChannelAsync(IVoiceChannel channel)
    {
        if (_voiceChannels.TryRemove(channel.Id, out var value))
        {
            value.cancellationTokenSource.Cancel();
            await value.recordingTask;
            await value.audioClient.StopAsync();
        }
    }

    private async Task RecordAudioContinuously(ulong channelId, IAudioClient audioClient, AudioBuffer audioBuffer, CancellationToken token)
    {
        try
        {
            var audioIn = audioClient.CreatePCMStream(AudioApplication.Voice);

            if (audioIn == null)
            {
                Console.WriteLine($"[ERROR] Failed to create PCM stream for channel {channelId}.");
                return;
            }
            else
            {
                Console.WriteLine($"[INFO] No bytes read from PCM stream for channel {channelId}.");
            }
            var buffer = new byte[3840];

            while (!token.IsCancellationRequested)
            {
                try
                {
                    int bytesRead = await audioIn.ReadAsync(buffer, 0, buffer.Length, token);
                    if (bytesRead > 0)
                    {
                        byte[] data = new byte[bytesRead];
                        Buffer.BlockCopy(buffer, 0, data, 0, bytesRead);
                        audioBuffer.Add(data);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected during cancellation. No action needed.
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Error during audio stream read for channel {channelId}: {ex.Message}");
                }
            }

            await audioIn.FlushAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Error in continuous recording for channel {channelId}: {ex.Message}");
        }
    }

    public async Task SaveBufferedAudioToFile(ulong channelId, string filePath, int duration)
    {
        if (_voiceChannels.TryGetValue(channelId, out var value))
        {
            try
            {
                int bytesToSave = duration * 48000 * 2; // calculate how many bytes correspond to the duration
                var bufferedData = value.audioBuffer.GetBufferedData(bytesToSave);
                await File.WriteAllBytesAsync(filePath, bufferedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to save buffered audio to file: {ex.Message}");
            }
        }
    }

    public async Task UploadAudioClipAsync(ISocketMessageChannel channel, string filePath)
    {
        try
        {
            await channel.SendFileAsync(filePath, "Here's your 15-second audio clip!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to upload audio clip: {ex.Message}");
        }
    }

    private async Task RetryAsync(Func<Task> action, TimeSpan delay, int maxAttempts)
    {
        int attempt = 0;
        while (attempt < maxAttempts)
        {
            try
            {
                await action();
                return;
            }
            catch (Exception ex)
            {
                attempt++;
                Console.WriteLine($"[RETRY] Attempt {attempt} failed: {ex.Message}");
                if (attempt < maxAttempts)
                {
                    await Task.Delay(delay);
                }
                else
                {
                    Console.WriteLine("[RETRY] Max attempts reached. Failing the operation.");
                }
            }
        }
    }
}