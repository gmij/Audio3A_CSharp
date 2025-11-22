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
    try {
        // 请求麦克风权限
        const constraints = {
            audio: {
                echoCancellation: enable3A,
                noiseSuppression: enable3A,
                autoGainControl: enable3A
            }
        };

        localStream = await navigator.mediaDevices.getUserMedia(constraints);
        
        // 创建音频上下文用于分析音频级别
        audioContext = new (window.AudioContext || window.webkitAudioContext)();
        const source = audioContext.createMediaStreamSource(localStream);
        analyser = audioContext.createAnalyser();
        analyser.fftSize = 256;
        source.connect(analyser);

        isActive = true;

        // 开始监控音频级别
        monitorAudioLevel();

        console.log('Call started for room:', roomId);
    } catch (error) {
        console.error('Failed to start call:', error);
        if (dotNetRef) {
            await dotNetRef.invokeMethodAsync('NotifyError', error.message);
        }
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
    if (!isActive || !analyser) return;

    const dataArray = new Uint8Array(analyser.frequencyBinCount);
    analyser.getByteFrequencyData(dataArray);

    // 计算平均音量
    let sum = 0;
    for (let i = 0; i < dataArray.length; i++) {
        sum += dataArray[i];
    }
    const average = sum / dataArray.length;
    const level = average / 255; // 归一化到 0-1

    // 通知 .NET 代码
    if (dotNetRef && !isMuted) {
        dotNetRef.invokeMethodAsync('NotifyAudioLevel', 'local', level);
    }

    // 继续监控
    if (isActive) {
        requestAnimationFrame(monitorAudioLevel);
    }
}
