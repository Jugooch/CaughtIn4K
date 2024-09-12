using System.Collections.Concurrent;
using System.IO;

// Manages a circular buffer to store audio data
public class AudioBuffer
{
    private readonly ConcurrentQueue<byte[]> _buffer; // Queue to hold audio chunks
    private readonly int _maxSize; // Maximum size of the buffer
    private int _currentSize; // Current size of the buffered data

    public AudioBuffer(int maxSize)
    {
        _buffer = new ConcurrentQueue<byte[]>();
        _maxSize = maxSize;
        _currentSize = 0;
    }

    // Adds new audio data to the buffer
    public void Add(byte[] data)
    {
        lock (_buffer)
        {
            _buffer.Enqueue(data);
            _currentSize += data.Length;

            // Remove older data if the buffer exceeds maximum size
            while (_currentSize > _maxSize && _buffer.TryDequeue(out byte[] removedData))
            {
                _currentSize -= removedData.Length;
            }
        }
    }

    // Retrieves all buffered audio data
    public byte[] GetBufferedData(int bytesToSave)
    {
        lock (_buffer)
        {
            // This is the core buffer retrieval logic optimized for partial buffer sizes
            var bufferedData = new byte[Math.Min(bytesToSave, _currentSize)];
            int offset = Math.Max(_currentSize - bytesToSave, 0);
            int bufferOffset = 0;

            foreach (var chunk in _buffer)
            {
                int copySize = Math.Min(chunk.Length, bufferedData.Length - bufferOffset);
                Buffer.BlockCopy(chunk, 0, bufferedData, bufferOffset, copySize);
                bufferOffset += copySize;
                if (bufferOffset >= bufferedData.Length)
                    break;
            }

            return bufferedData;
        }
    }
}