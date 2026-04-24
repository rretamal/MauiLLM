using System.Threading.Channels;
using Microsoft.ML.OnnxRuntimeGenAI;

namespace MauiLLM.Services;

public sealed class LocalLlmService : IDisposable
{
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly object _generationLock = new();
    private OgaHandle? _ogaHandle;
    private Model? _model;
    private Tokenizer? _tokenizer;
    private bool _isGenerating;
    private bool _disposed;

    public bool IsReady => _model is not null && _tokenizer is not null;

    public bool IsGenerating
    {
        get
        {
            lock (_generationLock)
            {
                return _isGenerating;
            }
        }
    }

    public async Task LoadModelAsync(string modelDirectory)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _loadLock.WaitAsync();
        try
        {
            if (IsGenerating)
            {
                throw new InvalidOperationException("Cannot reload the model while a response is being generated.");
            }

            await Task.Run(() =>
            {
                _tokenizer?.Dispose();
                _model?.Dispose();

                _ogaHandle ??= new OgaHandle();
                _model = new Model(modelDirectory);
                _tokenizer = new Tokenizer(_model);
            });
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async IAsyncEnumerable<string> GenerateAsync(
        string userPrompt,
        string systemPrompt = "You are a helpful assistant. Keep answers concise for a mobile user.")
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsReady)
        {
            throw new InvalidOperationException("Model not loaded.");
        }

        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            yield break;
        }

        lock (_generationLock)
        {
            if (_isGenerating)
            {
                throw new InvalidOperationException("A response is already being generated.");
            }

            _isGenerating = true;
        }

        var channel = Channel.CreateUnbounded<string>();
        var model = _model!;
        var tokenizer = _tokenizer!;
        var prompt = $"<|system|>{systemPrompt}<|end|><|user|>{userPrompt.Trim()}<|end|><|assistant|>";

        _ = Task.Run(() =>
        {
            try
            {
                var sequences = tokenizer.Encode(prompt);

                using var tokenizerStream = tokenizer.CreateStream();
                using var generatorParams = new GeneratorParams(model);
                generatorParams.SetSearchOption("max_length", 512);
                generatorParams.SetSearchOption("temperature", 0.7);
                generatorParams.SetSearchOption("past_present_share_buffer", false);

                using var generator = new Generator(model, generatorParams);
                generator.AppendTokenSequences(sequences);

                while (!generator.IsDone())
                {
                    generator.GenerateNextToken();
                    var token = generator.GetSequence(0)[^1];
                    var chunk = tokenizerStream.Decode(token);
                    if (!string.IsNullOrEmpty(chunk))
                    {
                        channel.Writer.TryWrite(chunk);
                    }
                }

                channel.Writer.TryComplete();
            }
            catch (Exception ex)
            {
                channel.Writer.TryComplete(ex);
            }
            finally
            {
                lock (_generationLock)
                {
                    _isGenerating = false;
                }
            }
        });

        await foreach (var chunk in channel.Reader.ReadAllAsync())
        {
            yield return chunk;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _tokenizer?.Dispose();
        _model?.Dispose();
        _ogaHandle?.Dispose();
        _loadLock.Dispose();
    }
}
