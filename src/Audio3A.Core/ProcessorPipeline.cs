using Microsoft.Extensions.Logging;
using Audio3A.Core.Processors;

namespace Audio3A.Core;

/// <summary>
/// 音频处理器管道 - 支持自定义处理顺序
/// 遵循责任链模式和开放/封闭原则（SOLID-O）
/// </summary>
public class ProcessorPipeline
{
    private readonly ILogger<ProcessorPipeline> _logger;
    private readonly List<IAudioProcessor> _processors;

    /// <summary>
    /// 初始化处理器管道
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public ProcessorPipeline(ILogger<ProcessorPipeline> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _processors = new List<IAudioProcessor>();
    }

    /// <summary>
    /// 添加处理器到管道
    /// </summary>
    /// <param name="processor">音频处理器</param>
    /// <returns>当前管道实例（支持链式调用）</returns>
    public ProcessorPipeline AddProcessor(IAudioProcessor processor)
    {
        if (processor == null)
            throw new ArgumentNullException(nameof(processor));

        _processors.Add(processor);
        _logger.LogDebug("Added processor to pipeline: {ProcessorType}", processor.GetType().Name);
        return this;
    }

    /// <summary>
    /// 执行管道处理
    /// </summary>
    /// <param name="input">输入音频缓冲区</param>
    /// <param name="speakerReference">扬声器参考信号（用于 AEC）</param>
    /// <returns>处理后的音频缓冲区</returns>
    public AudioBuffer Process(AudioBuffer input, AudioBuffer? speakerReference = null)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        _logger.LogTrace("Processing through pipeline with {Count} processors", _processors.Count);

        AudioBuffer current = input;
        
        foreach (var processor in _processors)
        {
            // AEC 处理器需要参考信号
            if (processor is AecProcessor aecProcessor)
            {
                current = aecProcessor.Process(current, speakerReference);
            }
            else
            {
                current = processor.Process(current);
            }
        }

        return current;
    }

    /// <summary>
    /// 重置管道中所有处理器
    /// </summary>
    public void Reset()
    {
        _logger.LogDebug("Resetting all processors in pipeline");
        foreach (var processor in _processors)
        {
            processor.Reset();
        }
    }

    /// <summary>
    /// 获取管道中的处理器数量
    /// </summary>
    public int Count => _processors.Count;

    /// <summary>
    /// 清空管道
    /// </summary>
    public void Clear()
    {
        _logger.LogDebug("Clearing pipeline");
        _processors.Clear();
    }
}
