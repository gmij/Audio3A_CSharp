// 音频通话 JavaScript 模块
let dotNetRef = null;
let localStream = null;
let audioContext = null;
let analyser = null;
let isMuted = false;
let isActive = false;

// 用于波形数据采集
let scriptProcessor = null;
let inputWaveformBuffer = [];
let processedWaveformBuffer = [];
const WAVEFORM_SAMPLE_SIZE = 200; // 波形数据点数量

export function initialize(dotNetReference) {
    dotNetRef = dotNetReference;
    console.log('Audio call module initialized');
}

export async function startCall(roomId, enable3A) {
    console.log('Starting call for room:', roomId, 'with 3A:', enable3A);
    
    try {
        // 检查浏览器支持
        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            throw new Error('浏览器不支持音频采集');
        }

        // 请求麦克风权限
        const constraints = {
            audio: {
                echoCancellation: enable3A,
                noiseSuppression: enable3A,
                autoGainControl: enable3A
            }
        };

        console.log('Requesting microphone access with constraints:', constraints);
        localStream = await navigator.mediaDevices.getUserMedia(constraints);
        console.log('Microphone access granted, tracks:', localStream.getAudioTracks().length);
        
        // 创建音频上下文用于分析音频级别
        audioContext = new (window.AudioContext || window.webkitAudioContext)();
        console.log('AudioContext created, state:', audioContext.state);
        
        const source = audioContext.createMediaStreamSource(localStream);
        analyser = audioContext.createAnalyser();
        analyser.fftSize = 256;
        
        // 创建 ScriptProcessor 用于采集波形数据
        // 注意：ScriptProcessor 已被弃用，但在这里我们用它来演示
        // 生产环境应该使用 AudioWorklet
        scriptProcessor = audioContext.createScriptProcessor(2048, 1, 1);
        
        source.connect(analyser);
        analyser.connect(scriptProcessor);
        scriptProcessor.connect(audioContext.destination);
        
        // 处理音频数据
        scriptProcessor.onaudioprocess = function(e) {
            if (!isActive || isMuted) return;
            
            const inputData = e.inputBuffer.getChannelData(0);
            const outputData = e.outputBuffer.getChannelData(0);
            
            // 采样输入波形数据
            collectWaveformData(inputData, inputWaveformBuffer, 'input');
            
            // 复制数据到输出（这里我们没有真正的3A处理，所以输出=输入）
            // 在实际应用中，这里应该是经过3A处理后的数据
            for (let i = 0; i < inputData.length; i++) {
                outputData[i] = inputData[i];
            }
            
            // 采样处理后的波形数据（这里模拟，实际上应该是3A处理后的）
            collectWaveformData(outputData, processedWaveformBuffer, 'processed');
        };
        
        console.log('Audio analyser and processor connected');

        isActive = true;

        // 开始监控音频级别
        monitorAudioLevel();
        console.log('Audio level monitoring started');

        console.log('Call started successfully for room:', roomId);
    } catch (error) {
        console.error('Failed to start call:', error);
        if (dotNetRef) {
            await dotNetRef.invokeMethodAsync('NotifyError', error.message);
        }
        throw error;
    }
}

export function endCall() {
    isActive = false;

    if (scriptProcessor) {
        scriptProcessor.disconnect();
        scriptProcessor = null;
    }

    if (localStream) {
        localStream.getTracks().forEach(track => track.stop());
        localStream = null;
    }

    if (audioContext) {
        audioContext.close();
        audioContext = null;
    }

    analyser = null;
    isMuted = false;
    inputWaveformBuffer = [];
    processedWaveformBuffer = [];

    console.log('Call ended');
}

export function setMuted(muted) {
    isMuted = muted;
    if (localStream) {
        localStream.getAudioTracks().forEach(track => {
            track.enabled = !muted;
        });
    }
}

function monitorAudioLevel() {
    if (!isActive || !analyser) {
        console.log('Monitoring stopped - isActive:', isActive, 'analyser:', !!analyser);
        return;
    }

    const dataArray = new Uint8Array(analyser.frequencyBinCount);
    analyser.getByteFrequencyData(dataArray);

    // 计算平均音量
    let sum = 0;
    for (let i = 0; i < dataArray.length; i++) {
        sum += dataArray[i];
    }
    const average = sum / dataArray.length;
    const level = average / 255; // 归一化到 0-1

    // 每100次监控打印一次（约每秒）
    if (!monitorAudioLevel.counter) monitorAudioLevel.counter = 0;
    monitorAudioLevel.counter++;
    if (monitorAudioLevel.counter % 60 === 0) {
        console.log('Audio level:', level.toFixed(3), 'isMuted:', isMuted);
    }

    // 通知 .NET 代码
    if (dotNetRef && !isMuted) {
        try {
            dotNetRef.invokeMethodAsync('NotifyAudioLevel', 'local', level);
        } catch (err) {
            console.error('Failed to notify audio level:', err);
        }
    }

    // 继续监控
    if (isActive) {
        requestAnimationFrame(monitorAudioLevel);
    }
}

// 采集波形数据
function collectWaveformData(audioData, buffer, type) {
    // 下采样到固定数量的点
    const step = Math.floor(audioData.length / WAVEFORM_SAMPLE_SIZE);
    const samples = [];
    
    for (let i = 0; i < WAVEFORM_SAMPLE_SIZE; i++) {
        const index = i * step;
        if (index < audioData.length) {
            // 转换为 0-255 范围
            const normalized = Math.abs(audioData[index]);
            samples.push(Math.min(255, Math.floor(normalized * 255)));
        } else {
            samples.push(0);
        }
    }
    
    // 每隔一定帧数发送波形数据
    if (!collectWaveformData.counter) collectWaveformData.counter = {};
    if (!collectWaveformData.counter[type]) collectWaveformData.counter[type] = 0;
    collectWaveformData.counter[type]++;
    
    // 每 10 帧发送一次波形数据（约每秒几次）
    if (collectWaveformData.counter[type] % 10 === 0) {
        if (dotNetRef && !isMuted) {
            try {
                const uint8Array = new Uint8Array(samples);
                if (type === 'input') {
                    dotNetRef.invokeMethodAsync('NotifyInputWaveform', Array.from(uint8Array));
                } else if (type === 'processed') {
                    dotNetRef.invokeMethodAsync('NotifyProcessedWaveform', Array.from(uint8Array));
                }
            } catch (err) {
                console.error(`Failed to send ${type} waveform:`, err);
            }
        }
    }
}
