// 波形可视化 JavaScript 模块
const waveformData = new Map();

export function initWaveform(canvas, width, height, color, backgroundColor) {
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // 存储配置
    waveformData.set(canvas, {
        color: color,
        backgroundColor: backgroundColor,
        width: width,
        height: height,
        data: new Array(200).fill(0) // 默认200个数据点
    });

    // 绘制初始背景
    drawWaveform(canvas);
}

export function updateWaveform(canvas, dataArray) {
    const config = waveformData.get(canvas);
    if (!config) return;

    // 更新数据
    config.data = Array.from(dataArray);
    drawWaveform(canvas);
}

export function updateWaveformFromBytes(canvas, byteArray) {
    const config = waveformData.get(canvas);
    if (!config) return;

    // 将 byte array 转换为归一化的 float array
    const normalized = Array.from(byteArray).map(b => b / 255.0);
    config.data = normalized;
    drawWaveform(canvas);
}

function drawWaveform(canvas) {
    const config = waveformData.get(canvas);
    if (!config) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const { width, height, data, color, backgroundColor } = config;

    // 清除画布
    ctx.fillStyle = backgroundColor;
    ctx.fillRect(0, 0, width, height);

    // 绘制中心线
    ctx.strokeStyle = 'rgba(255, 255, 255, 0.2)';
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(0, height / 2);
    ctx.lineTo(width, height / 2);
    ctx.stroke();

    // 绘制波形
    if (data.length === 0) return;

    const sliceWidth = width / data.length;
    const halfHeight = height / 2;

    // 绘制填充区域
    ctx.fillStyle = color + '40'; // 添加透明度
    ctx.beginPath();
    ctx.moveTo(0, halfHeight);

    for (let i = 0; i < data.length; i++) {
        const x = i * sliceWidth;
        const value = Math.max(0, Math.min(1, data[i])); // 限制在 0-1 之间
        const y = halfHeight - (value * halfHeight);
        
        if (i === 0) {
            ctx.moveTo(x, y);
        } else {
            ctx.lineTo(x, y);
        }
    }

    // 镜像到下半部分
    for (let i = data.length - 1; i >= 0; i--) {
        const x = i * sliceWidth;
        const value = Math.max(0, Math.min(1, data[i]));
        const y = halfHeight + (value * halfHeight);
        ctx.lineTo(x, y);
    }

    ctx.closePath();
    ctx.fill();

    // 绘制波形线
    ctx.strokeStyle = color;
    ctx.lineWidth = 2;
    ctx.beginPath();

    for (let i = 0; i < data.length; i++) {
        const x = i * sliceWidth;
        const value = Math.max(0, Math.min(1, data[i]));
        const y = halfHeight - (value * halfHeight);
        
        if (i === 0) {
            ctx.moveTo(x, y);
        } else {
            ctx.lineTo(x, y);
        }
    }

    ctx.stroke();

    // 绘制镜像线
    ctx.beginPath();
    for (let i = 0; i < data.length; i++) {
        const x = i * sliceWidth;
        const value = Math.max(0, Math.min(1, data[i]));
        const y = halfHeight + (value * halfHeight);
        
        if (i === 0) {
            ctx.moveTo(x, y);
        } else {
            ctx.lineTo(x, y);
        }
    }

    ctx.stroke();
}
