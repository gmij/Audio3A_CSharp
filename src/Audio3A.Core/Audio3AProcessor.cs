using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Audio3A.Core.Processors;

namespace Audio3A.Core;

/// <summary>
/// Main Audio 3A processor that combines AEC, AGC, and ANS
/// 支持可配置的处理器执行顺序（遵循开放/封闭原则）
/// </summary>
public class Audio3AProcessor : IDisposable
{
    private readonly ILogger<Audio3AProcessor> _logger;
    private readonly Audio3AConfig _config;
    private readonly AecProcessor? _aecProcessor;
    private readonly AgcProcessor? _agcProcessor;
    private readonly AnsProcessor? _ansProcessor;
    private readonly ProcessorPipeline _pipeline;
    private bool _disposed;

    /// <summary>
    /// Initializes a new Audio3A processor with dependency injection
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="config">Configuration for 3A processing</param>
    /// <param name="serviceProvider">Service provider for resolving optional processors</param>
    public Audio3AProcessor(
        ILogger<Audio3AProcessor> logger,
        Audio3AConfig config,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));

        // Resolve processors from DI container only if enabled in config
        if (_config.EnableAec)
        {
            _aecProcessor = serviceProvider.GetService<AecProcessor>();
        }

        if (_config.EnableAgc)
        {
            _agcProcessor = serviceProvider.GetService<AgcProcessor>();
        }

        if (_config.EnableAns)
        {
            _ansProcessor = serviceProvider.GetService<AnsProcessor>();
        }

        // Create pipeline based on configured processing order
        var pipelineLogger = serviceProvider.GetRequiredService<ILogger<ProcessorPipeline>>();
        _pipeline = new ProcessorPipeline(pipelineLogger);
        ConfigurePipeline();

        _logger.LogInformation(
            "Audio3A processor initialized: AEC={AecEnabled}, AGC={AgcEnabled}, ANS={AnsEnabled}, Order={ProcessingOrder}, SampleRate={SampleRate}",
            _config.EnableAec, _config.EnableAgc, _config.EnableAns, _config.ProcessingOrder, _config.SampleRate);
    }

    /// <summary>
    /// 根据配置设置处理器管道顺序
    /// </summary>
    private void ConfigurePipeline()
    {
        switch (_config.ProcessingOrder)
        {
            case ProcessingOrder.Standard:
                // WebRTC 推荐顺序：AEC -> ANS -> AGC
                AddProcessorToPipeline(_aecProcessor);
                AddProcessorToPipeline(_ansProcessor);
                AddProcessorToPipeline(_agcProcessor);
                break;

            case ProcessingOrder.NoiseSuppressFirst:
                // 噪声优先：ANS -> AEC -> AGC
                AddProcessorToPipeline(_ansProcessor);
                AddProcessorToPipeline(_aecProcessor);
                AddProcessorToPipeline(_agcProcessor);
                break;

            case ProcessingOrder.GainControlFirst:
                // 增益优先：AGC -> AEC -> ANS
                AddProcessorToPipeline(_agcProcessor);
                AddProcessorToPipeline(_aecProcessor);
                AddProcessorToPipeline(_ansProcessor);
                break;

            case ProcessingOrder.Custom:
                // 自定义模式：用户需要手动配置 Pipeline
                _logger.LogWarning("Custom processing order selected but not configured. Using standard order.");
                goto case ProcessingOrder.Standard;

            default:
                _logger.LogWarning("Unknown processing order {Order}, using standard order", _config.ProcessingOrder);
                goto case ProcessingOrder.Standard;
        }
    }

    /// <summary>
    /// 添加处理器到管道（如果非空）
    /// </summary>
    private void AddProcessorToPipeline(IAudioProcessor? processor)
    {
        if (processor != null)
        {
            _pipeline.AddProcessor(processor);
        }
    }

    /// <summary>
    /// Process audio with 3A algorithms using configured pipeline order
    /// </summary>
    /// <param name="micInput">Microphone input buffer</param>
    /// <param name="speakerReference">Speaker reference signal for echo cancellation (optional)</param>
    /// <returns>Processed audio buffer</returns>
    public AudioBuffer Process(AudioBuffer micInput, AudioBuffer? speakerReference = null)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(Audio3AProcessor));
        }

        _logger.LogTrace("Processing audio buffer: Length={Length} samples", micInput.Length);

        return _pipeline.Process(micInput, speakerReference);
    }

    /// <summary>
    /// Process 16-bit PCM audio data
    /// </summary>
    /// <param name="micInputPcm">Microphone input as 16-bit PCM</param>
    /// <param name="speakerReferencePcm">Speaker reference as 16-bit PCM (optional)</param>
    /// <returns>Processed audio as 16-bit PCM</returns>
    public short[] ProcessInt16(short[] micInputPcm, short[]? speakerReferencePcm = null)
    {
        AudioBuffer micBuffer = AudioBuffer.FromInt16(micInputPcm, _config.Channels, _config.SampleRate);
        AudioBuffer? speakerBuffer = speakerReferencePcm != null
            ? AudioBuffer.FromInt16(speakerReferencePcm, _config.Channels, _config.SampleRate)
            : null;

        AudioBuffer processed = Process(micBuffer, speakerBuffer);
        return processed.ToInt16();
    }

    /// <summary>
    /// Reset all processors to their initial state
    /// </summary>
    public void Reset()
    {
        _logger.LogDebug("Resetting all processors");
        _pipeline.Reset();
    }

    /// <summary>
    /// Gets the current configuration
    /// </summary>
    public Audio3AConfig Config => _config;

    /// <summary>
    /// Gets the processor pipeline (for custom configuration when ProcessingOrder is Custom)
    /// </summary>
    public ProcessorPipeline Pipeline => _pipeline;

    public void Dispose()
    {
        if (!_disposed)
        {
            _logger.LogDebug("Disposing Audio3A processor");
            Reset();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
