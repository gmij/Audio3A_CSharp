namespace Audio3A.RoomManagement.Models;

/// <summary>
/// 音频帧数据
/// </summary>
public class AudioFrame
{
    /// <summary>
    /// 参与者 ID
    /// </summary>
    public string ParticipantId { get; }

    /// <summary>
    /// 音频数据（16 位 PCM）
    /// </summary>
    public short[] Data { get; }

    /// <summary>
    /// 采样率
    /// </summary>
    public int SampleRate { get; }

    /// <summary>
    /// 声道数
    /// </summary>
    public int Channels { get; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// 序列号（用于排序和丢包检测）
    /// </summary>
    public long SequenceNumber { get; }

    public AudioFrame(string participantId, short[] data, int sampleRate, int channels, long sequenceNumber)
    {
        if (string.IsNullOrWhiteSpace(participantId))
            throw new ArgumentException("Participant ID cannot be empty", nameof(participantId));

        if (data == null || data.Length == 0)
            throw new ArgumentException("Audio data cannot be empty", nameof(data));

        ParticipantId = participantId;
        Data = data;
        SampleRate = sampleRate;
        Channels = channels;
        Timestamp = DateTime.UtcNow;
        SequenceNumber = sequenceNumber;
    }
}
