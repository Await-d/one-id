/**
 * 设备指纹采集工具
 * 用于生成唯一的设备标识
 */

export interface DeviceFingerprint {
  fingerprint: string;
  deviceInfo: {
    browser?: string;
    browserVersion?: string;
    operatingSystem?: string;
    osVersion?: string;
    deviceType?: string;
    screenResolution?: string;
    timeZone?: string;
    language?: string;
    platform?: string;
  };
}

/**
 * 获取浏览器信息
 */
function getBrowserInfo(): { name: string; version: string } {
  const ua = navigator.userAgent;
  let browserName = "Unknown";
  let browserVersion = "Unknown";

  // Chrome
  if (ua.indexOf("Chrome") > -1 && ua.indexOf("Edg") === -1) {
    browserName = "Chrome";
    const match = ua.match(/Chrome\/(\d+\.\d+)/);
    if (match) browserVersion = match[1];
  }
  // Edge
  else if (ua.indexOf("Edg") > -1) {
    browserName = "Edge";
    const match = ua.match(/Edg\/(\d+\.\d+)/);
    if (match) browserVersion = match[1];
  }
  // Firefox
  else if (ua.indexOf("Firefox") > -1) {
    browserName = "Firefox";
    const match = ua.match(/Firefox\/(\d+\.\d+)/);
    if (match) browserVersion = match[1];
  }
  // Safari
  else if (ua.indexOf("Safari") > -1 && ua.indexOf("Chrome") === -1) {
    browserName = "Safari";
    const match = ua.match(/Version\/(\d+\.\d+)/);
    if (match) browserVersion = match[1];
  }

  return { name: browserName, version: browserVersion };
}

/**
 * 获取操作系统信息
 */
function getOSInfo(): { name: string; version: string } {
  const ua = navigator.userAgent;
  const platform = navigator.platform;
  let osName = "Unknown";
  let osVersion = "Unknown";

  if (ua.indexOf("Windows NT 10.0") > -1) {
    osName = "Windows";
    osVersion = "10";
  } else if (ua.indexOf("Windows NT 6.3") > -1) {
    osName = "Windows";
    osVersion = "8.1";
  } else if (ua.indexOf("Windows NT 6.2") > -1) {
    osName = "Windows";
    osVersion = "8";
  } else if (ua.indexOf("Windows NT 6.1") > -1) {
    osName = "Windows";
    osVersion = "7";
  } else if (ua.indexOf("Mac OS X") > -1) {
    osName = "macOS";
    const match = ua.match(/Mac OS X (\d+[._]\d+)/);
    if (match) osVersion = match[1].replace("_", ".");
  } else if (ua.indexOf("Linux") > -1) {
    osName = "Linux";
  } else if (ua.indexOf("Android") > -1) {
    osName = "Android";
    const match = ua.match(/Android (\d+\.\d+)/);
    if (match) osVersion = match[1];
  } else if (ua.indexOf("iPhone") > -1 || ua.indexOf("iPad") > -1) {
    osName = "iOS";
    const match = ua.match(/OS (\d+_\d+)/);
    if (match) osVersion = match[1].replace("_", ".");
  }

  return { name: osName, version: osVersion };
}

/**
 * 获取设备类型
 */
function getDeviceType(): string {
  const ua = navigator.userAgent;
  
  if (/(tablet|ipad|playbook|silk)|(android(?!.*mobi))/i.test(ua)) {
    return "Tablet";
  }
  if (/Mobile|Android|iP(hone|od)|IEMobile|BlackBerry|Kindle|Silk-Accelerated|(hpw|web)OS|Opera M(obi|ini)/.test(ua)) {
    return "Mobile";
  }
  return "Desktop";
}

/**
 * 获取屏幕分辨率
 */
function getScreenResolution(): string {
  return `${window.screen.width}x${window.screen.height}`;
}

/**
 * 获取时区
 */
function getTimeZone(): string {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone;
  } catch {
    const offset = -new Date().getTimezoneOffset() / 60;
    return `UTC${offset >= 0 ? "+" : ""}${offset}`;
  }
}

/**
 * 获取语言
 */
function getLanguage(): string {
  return navigator.language || "en-US";
}

/**
 * 生成简单的哈希码
 */
function simpleHash(str: string): string {
  let hash = 0;
  for (let i = 0; i < str.length; i++) {
    const char = str.charCodeAt(i);
    hash = (hash << 5) - hash + char;
    hash = hash & hash; // Convert to 32bit integer
  }
  return Math.abs(hash).toString(36);
}

/**
 * 生成设备指纹
 */
export async function generateDeviceFingerprint(): Promise<DeviceFingerprint> {
  const browser = getBrowserInfo();
  const os = getOSInfo();
  const deviceType = getDeviceType();
  const screenResolution = getScreenResolution();
  const timeZone = getTimeZone();
  const language = getLanguage();
  const platform = navigator.platform;

  // 收集所有特征
  const features = [
    navigator.userAgent,
    screenResolution,
    window.screen.colorDepth.toString(),
    timeZone,
    language,
    platform,
    navigator.hardwareConcurrency?.toString() || "unknown",
    navigator.maxTouchPoints?.toString() || "0",
  ];

  // 生成指纹
  const fingerprintString = features.join("|");
  const fingerprint = simpleHash(fingerprintString);

  return {
    fingerprint,
    deviceInfo: {
      browser: browser.name,
      browserVersion: browser.version,
      operatingSystem: os.name,
      osVersion: os.version,
      deviceType,
      screenResolution,
      timeZone,
      language,
      platform,
    },
  };
}

/**
 * 获取设备指纹（带缓存）
 */
let cachedFingerprint: DeviceFingerprint | null = null;

export async function getDeviceFingerprint(): Promise<DeviceFingerprint> {
  if (!cachedFingerprint) {
    cachedFingerprint = await generateDeviceFingerprint();
  }
  return cachedFingerprint;
}

