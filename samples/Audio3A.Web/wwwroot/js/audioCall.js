// 音频通话 JavaScript 模块
let dotNetRef = null;
let localStream = null;
let audioContext = null;
let analyser = null;
let isMuted = false;
let isActive = false;

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
        source.connect(analyser);
        console.log('Audio analyser connected');

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
