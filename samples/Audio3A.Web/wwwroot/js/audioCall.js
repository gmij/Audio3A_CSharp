// 音频采集 JavaScript 模块 - 只负责采集麦克风音频
let dotNetRef = null;
let localStream = null;
let audioContext = null;
let analyser = null;
let scriptProcessor = null;
let isMuted = false;
let isActive = false;

// 采集配置
const SAMPLE_RATE = 48000; // 浏览器标准采样率
const BUFFER_SIZE = 2048; // ScriptProcessor 缓冲区大小
const AUDIO_DATA_INTERVAL_MS = 20; // 音频数据发送间隔（毫秒）

export function initialize(dotNetReference) {
    dotNetRef = dotNetReference;
    console.log('Audio capture module initialized');
}

/**
 * 开始音频采集
 * @param {string} roomId - 房间ID
 */
export async function startCapture(roomId) {
    console.log('Starting audio capture for room:', roomId);
    
    try {
        // 检查浏览器支持
        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            throw new Error('浏览器不支持音频采集');
        }

        // 请求麦克风权限（不启用浏览器3A，因为后端会处理）
        const constraints = {
            audio: {
                echoCancellation: false,  // 后端处理
                noiseSuppression: false,  // 后端处理
                autoGainControl: false,   // 后端处理
                sampleRate: SAMPLE_RATE,
                channelCount: 1          // 单声道
            }
        };

        console.log('Requesting microphone access:', constraints);
        localStream = await navigator.mediaDevices.getUserMedia(constraints);
        console.log('Microphone access granted');
        
        // 创建音频上下文
        audioContext = new (window.AudioContext || window.webkitAudioContext)({
            sampleRate: SAMPLE_RATE
        });
        console.log('AudioContext created, sample rate:', audioContext.sampleRate);
        
        const source = audioContext.createMediaStreamSource(localStream);
        
        // 创建分析器用于音量监控
        analyser = audioContext.createAnalyser();
        analyser.fftSize = 256;
        
        // 创建 ScriptProcessor 用于采集音频数据
        scriptProcessor = audioContext.createScriptProcessor(BUFFER_SIZE, 1, 1);
        
        source.connect(analyser);
        analyser.connect(scriptProcessor);
        scriptProcessor.connect(audioContext.destination);
        
        // 音频数据缓冲区
        let audioDataBuffer = [];
        let lastSendTime = Date.now();
        
        // 处理音频数据
        scriptProcessor.onaudioprocess = function(e) {
            if (!isActive || isMuted) {
                return;
            }
            
            const inputData = e.inputBuffer.getChannelData(0);
            const outputData = e.outputBuffer.getChannelData(0);
            
            // 直接复制到输出（用于本地监听）
            for (let i = 0; i < inputData.length; i++) {
                outputData[i] = inputData[i];
            }
            
            // 收集音频数据
            const samples = Array.from(inputData);
            audioDataBuffer.push(...samples);
            
            // 每隔一定时间发送音频数据到 C#
            const now = Date.now();
            if (now - lastSendTime >= AUDIO_DATA_INTERVAL_MS && audioDataBuffer.length > 0) {
                if (dotNetRef) {
                    try {
                        // 发送音频数据到 C# (Float32Array)
                        dotNetRef.invokeMethodAsync('NotifyAudioData', audioDataBuffer);
                    } catch (err) {
                        console.error('Failed to send audio data:', err);
                    }
                }
                
                // 清空缓冲区
                audioDataBuffer = [];
                lastSendTime = now;
            }
        };
        
        isActive = true;
        
        // 开始监控音频级别
        monitorAudioLevel();
        console.log('Audio capture started successfully');
        
    } catch (error) {
        console.error('Failed to start audio capture:', error);
        if (dotNetRef) {
            await dotNetRef.invokeMethodAsync('NotifyError', error.message);
        }
        throw error;
    }
}

/**
 * 停止音频采集
 */
export function stopCapture() {
    console.log('Stopping audio capture');
    isActive = false;

    if (scriptProcessor) {
        scriptProcessor.disconnect();
        scriptProcessor = null;
    }

    if (analyser) {
        analyser.disconnect();
        analyser = null;
    }

    if (localStream) {
        localStream.getTracks().forEach(track => track.stop());
        localStream = null;
    }

    if (audioContext) {
        audioContext.close();
        audioContext = null;
    }

    isMuted = false;
    console.log('Audio capture stopped');
}

/**
 * 设置静音状态
 * @param {boolean} muted - 是否静音
 */
export function setMuted(muted) {
    isMuted = muted;
    if (localStream) {
        localStream.getAudioTracks().forEach(track => {
            track.enabled = !muted;
        });
    }
    console.log('Muted:', muted);
}

/**
 * 监控音频级别
 */
function monitorAudioLevel() {
    if (!isActive || !analyser) {
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

    // 通知 C# (每 100ms 通知一次)
    if (!monitorAudioLevel.lastNotifyTime) {
        monitorAudioLevel.lastNotifyTime = 0;
    }
    
    const now = Date.now();
    if (now - monitorAudioLevel.lastNotifyTime >= 100) {
        if (dotNetRef && !isMuted) {
            try {
                dotNetRef.invokeMethodAsync('NotifyAudioLevel', 'local', level);
            } catch (err) {
                // 忽略错误
            }
        }
        monitorAudioLevel.lastNotifyTime = now;
    }

    // 继续监控
    if (isActive) {
        requestAnimationFrame(monitorAudioLevel);
    }
}
