namespace Audio3A.Core;

/// <summary>
/// 信号处理辅助方法集合
/// 提供通用的信号处理功能，避免代码重复（遵循 DRY 原则）
/// </summary>
public static class SignalProcessingHelpers
{
    /// <summary>
    /// 计算帧能量
    /// </summary>
    /// <param name="samples">音频样本数组</param>
    /// <returns>归一化的帧能量</returns>
    public static float CalculateFrameEnergy(float[] samples)
    {
        if (samples == null || samples.Length == 0)
            return 0.0f;
            
        float energy = 0.0f;
        for (int i = 0; i < samples.Length; i++)
        {
            energy += samples[i] * samples[i];
        }
        return energy / samples.Length;
    }

    /// <summary>
    /// 计算信噪比 (SNR)
    /// </summary>
    /// <param name="signalPower">信号功率（必须非负）</param>
    /// <param name="noisePower">噪声功率（必须非负）</param>
    /// <returns>信噪比</returns>
    public static float CalculateSnr(float signalPower, float noisePower)
    {
        // 确保功率值非负
        signalPower = Math.Max(0.0f, signalPower);
        noisePower = Math.Max(0.0f, noisePower);
        
        return signalPower / (noisePower + 1e-10f);
    }

    /// <summary>
    /// 软限幅器 - 防止削波（WebRTC 风格）
    /// </summary>
    /// <param name="sample">输入样本</param>
    /// <param name="threshold">软膝点阈值（必须在 0.0 到 1.0 之间，默认 0.8）</param>
    /// <returns>限幅后的样本</returns>
    public static float SoftLimiter(float sample, float threshold = 0.8f)
    {
        // 确保阈值在有效范围内
        threshold = Clamp(threshold, 0.0f, 0.99f);
        
        if (Math.Abs(sample) > threshold)
        {
            float sign = sample >= 0 ? 1.0f : -1.0f;
            float absSample = Math.Abs(sample);
            // 软膝压缩
            float compressed = threshold + (1.0f - threshold) * (float)Math.Tanh((absSample - threshold) / (1.0f - threshold));
            return sign * Math.Min(compressed, 1.0f);
        }

        return Math.Max(-1.0f, Math.Min(1.0f, sample));
    }

    /// <summary>
    /// 限制值在指定范围内
    /// </summary>
    /// <param name="value">输入值</param>
    /// <param name="min">最小值</param>
    /// <param name="max">最大值</param>
    /// <returns>限制后的值</returns>
    public static float Clamp(float value, float min, float max)
    {
        return Math.Max(min, Math.Min(value, max));
    }

    /// <summary>
    /// 生成舒适噪声（WebRTC 风格）
    /// 使用 Random.Shared 确保线程安全
    /// </summary>
    /// <param name="amplitude">噪声幅度（默认 0.001）</param>
    /// <returns>噪声样本</returns>
    public static float GenerateComfortNoise(float amplitude = 0.001f)
    {
        return amplitude * (float)(2.0 * (Random.Shared.NextDouble() - 0.5));
    }
}
