using Microsoft.Extensions.Logging;
using Audio3A.RoomManagement.Models;

namespace Audio3A.RoomManagement.Audio;

/// <summary>
/// 音频混音器 - 混合多个音频流
/// </summary>
public class AudioMixer
{
    private readonly ILogger<AudioMixer> _logger;

    public AudioMixer(ILogger<AudioMixer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 混合多个音频帧（排除指定参与者）
    /// </summary>
    /// <param name="frames">音频帧集合</param>
    /// <param name="excludeParticipantId">要排除的参与者 ID（避免自己听到自己的声音）</param>
    /// <returns>混合后的音频数据</returns>
    public short[] Mix(IEnumerable<AudioFrame> frames, string? excludeParticipantId = null)
    {
        var frameList = frames.Where(f => f.ParticipantId != excludeParticipantId).ToList();

        if (frameList.Count == 0)
        {
            _logger.LogTrace("No frames to mix");
            return Array.Empty<short>();
        }

        if (frameList.Count == 1)
        {
            _logger.LogTrace("Only one frame, returning directly");
            return frameList[0].Data;
        }

        // 确保所有帧长度一致
        int frameLength = frameList[0].Data.Length;
        if (frameList.Any(f => f.Data.Length != frameLength))
        {
            _logger.LogWarning("Frame lengths are inconsistent, using minimum length");
            frameLength = frameList.Min(f => f.Data.Length);
        }

        short[] mixedData = new short[frameLength];

        // 使用加权平均法混音
        for (int i = 0; i < frameLength; i++)
        {
            int sum = 0;
            foreach (var frame in frameList)
            {
                sum += frame.Data[i];
            }

            // 限幅处理，防止溢出
            int mixed = sum / frameList.Count;
            mixedData[i] = (short)Math.Clamp(mixed, short.MinValue, short.MaxValue);
        }

        _logger.LogTrace("Mixed {Count} audio frames into {Length} samples", frameList.Count, frameLength);
        return mixedData;
    }

    /// <summary>
    /// 高级混音：使用能量感知的自动增益调整
    /// </summary>
    /// <param name="frames">音频帧集合</param>
    /// <param name="excludeParticipantId">要排除的参与者 ID</param>
    /// <returns>混合后的音频数据</returns>
    public short[] MixWithAutoGain(IEnumerable<AudioFrame> frames, string? excludeParticipantId = null)
    {
        var frameList = frames.Where(f => f.ParticipantId != excludeParticipantId).ToList();

        if (frameList.Count == 0)
        {
            return Array.Empty<short>();
        }

        if (frameList.Count == 1)
        {
            return frameList[0].Data;
        }

        int frameLength = frameList[0].Data.Length;
        if (frameList.Any(f => f.Data.Length != frameLength))
        {
            frameLength = frameList.Min(f => f.Data.Length);
        }

        short[] mixedData = new short[frameLength];

        // 计算每个帧的能量
        var frameEnergies = frameList.Select(f => CalculateEnergy(f.Data)).ToList();
        double totalEnergy = frameEnergies.Sum();

        if (totalEnergy == 0)
        {
            _logger.LogTrace("Total energy is zero, returning silence");
            return mixedData;
        }

        // 使用能量加权混音
        for (int i = 0; i < frameLength; i++)
        {
            double sum = 0;
            for (int j = 0; j < frameList.Count; j++)
            {
                double weight = frameEnergies[j] / totalEnergy;
                sum += frameList[j].Data[i] * weight;
            }

            // 限幅处理
            mixedData[i] = (short)Math.Clamp((int)sum, short.MinValue, short.MaxValue);
        }

        _logger.LogTrace("Mixed {Count} audio frames with auto-gain into {Length} samples", 
            frameList.Count, frameLength);
        return mixedData;
    }

    /// <summary>
    /// 计算音频能量（RMS）
    /// </summary>
    private static double CalculateEnergy(short[] data)
    {
        if (data.Length == 0) return 0;

        double sum = 0;
        foreach (short sample in data)
        {
            sum += sample * sample;
        }
        return Math.Sqrt(sum / data.Length);
    }
}
