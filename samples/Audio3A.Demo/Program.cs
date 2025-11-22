using Audio3A.Core;
using Audio3A.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Build host with dependency injection
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Register Audio 3A services
        services.AddAudio3A(config =>
        {
            config.EnableAec = true;
            config.EnableAgc = true;
            config.EnableAns = true;
            config.SampleRate = 16000;
            config.Channels = 1;
            config.FrameSize = 160;
            config.AgcTargetLevel = 0.5f;
            config.AnsNoiseReductionDb = 20.0f;
        });
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .Build();

// Run demo
using (var scope = host.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var processor = services.GetRequiredService<Audio3AProcessor>();
    var config = services.GetRequiredService<Audio3AConfig>();

    logger.LogInformation("Audio 3A SDK Demo");
    logger.LogInformation("=================\n");

    Console.WriteLine("Configuration:");
    Console.WriteLine($"  Sample Rate: {config.SampleRate} Hz");
    Console.WriteLine($"  Channels: {config.Channels}");
    Console.WriteLine($"  Frame Size: {config.FrameSize} samples");
    Console.WriteLine($"  AEC: {(config.EnableAec ? "Enabled" : "Disabled")}");
    Console.WriteLine($"  AGC: {(config.EnableAgc ? "Enabled" : "Disabled")} (Target: {config.AgcTargetLevel})");
    Console.WriteLine($"  ANS: {(config.EnableAns ? "Enabled" : "Disabled")} (Reduction: {config.AnsNoiseReductionDb} dB)");
    Console.WriteLine();

    // Example 1: Process a simple audio buffer
    logger.LogInformation("Example 1: Processing audio buffer");
    Console.WriteLine("Example 1: Processing audio buffer");
    Console.WriteLine("-----------------------------------");

    // Create a synthetic audio signal (sine wave with noise)
    int sampleCount = 160; // 10ms at 16kHz
    float[] inputSamples = new float[sampleCount];
    Random random = new Random(42);

    for (int i = 0; i < sampleCount; i++)
    {
        // Sine wave (1kHz) with noise
        float signal = 0.3f * (float)Math.Sin(2 * Math.PI * 1000 * i / config.SampleRate);
        float noise = 0.05f * (float)(random.NextDouble() * 2 - 1);
        inputSamples[i] = signal + noise;
    }

    var inputBuffer = new AudioBuffer(inputSamples, config.Channels, config.SampleRate);

    Console.WriteLine($"Input: {sampleCount} samples");
    Console.WriteLine($"Input RMS: {CalculateRms(inputBuffer.Samples):F4}");

    // Process the buffer
    var outputBuffer = processor.Process(inputBuffer);

    Console.WriteLine($"Output: {outputBuffer.Length} samples");
    Console.WriteLine($"Output RMS: {CalculateRms(outputBuffer.Samples):F4}");
    Console.WriteLine();

    // Example 2: Process 16-bit PCM data
    logger.LogInformation("Example 2: Processing 16-bit PCM data");
    Console.WriteLine("Example 2: Processing 16-bit PCM data");
    Console.WriteLine("--------------------------------------");

    // Convert to 16-bit PCM
    short[] inputPcm = inputBuffer.ToInt16();
    Console.WriteLine($"Input PCM: {inputPcm.Length} samples");

    // Process PCM data
    short[] outputPcm = processor.ProcessInt16(inputPcm);
    Console.WriteLine($"Output PCM: {outputPcm.Length} samples");
    Console.WriteLine();

    // Example 3: Process with echo reference
    logger.LogInformation("Example 3: Processing with AEC (echo reference)");
    Console.WriteLine("Example 3: Processing with AEC (echo reference)");
    Console.WriteLine("------------------------------------------------");

    // Create a reference signal (simulated speaker output)
    float[] referenceSamples = new float[sampleCount];
    for (int i = 0; i < sampleCount; i++)
    {
        referenceSamples[i] = 0.5f * (float)Math.Sin(2 * Math.PI * 500 * i / config.SampleRate);
    }

    var referenceBuffer = new AudioBuffer(referenceSamples, config.Channels, config.SampleRate);

    // Create mic input with echo (reference + original signal)
    float[] micWithEcho = new float[sampleCount];
    for (int i = 0; i < sampleCount; i++)
    {
        micWithEcho[i] = inputSamples[i] + 0.3f * referenceSamples[i]; // Add echo
    }

    var micBuffer = new AudioBuffer(micWithEcho, config.Channels, config.SampleRate);

    Console.WriteLine($"Mic input RMS (with echo): {CalculateRms(micBuffer.Samples):F4}");

    // Process with AEC
    var aecOutput = processor.Process(micBuffer, referenceBuffer);

    Console.WriteLine($"AEC output RMS: {CalculateRms(aecOutput.Samples):F4}");
    Console.WriteLine();

    // Example 4: Reset processor
    logger.LogInformation("Example 4: Reset processor state");
    Console.WriteLine("Example 4: Reset processor state");
    Console.WriteLine("---------------------------------");
    processor.Reset();
    Console.WriteLine("Processor state has been reset.");
    Console.WriteLine();

    logger.LogInformation("Demo completed successfully!");
    Console.WriteLine("Demo completed successfully!");
    Console.WriteLine("\nNote: This is a demonstration of the Audio 3A SDK API.");
    Console.WriteLine("For real-world usage, integrate with audio capture/playback libraries.");
}

static float CalculateRms(float[] samples)
{
    float sum = 0;
    foreach (float sample in samples)
    {
        sum += sample * sample;
    }
    return (float)Math.Sqrt(sum / samples.Length);
}
