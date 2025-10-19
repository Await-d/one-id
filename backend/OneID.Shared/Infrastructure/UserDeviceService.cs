using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneID.Shared.Data;
using OneID.Shared.Domain;

namespace OneID.Shared.Infrastructure;

public class UserDeviceService : IUserDeviceService
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserDeviceService> _logger;

    public UserDeviceService(AppDbContext context, ILogger<UserDeviceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserDevice> RecordDeviceAsync(
        Guid userId,
        string deviceFingerprint,
        DeviceInfo deviceInfo,
        string? ipAddress = null,
        string? location = null)
    {
        // 查找现有设备
        var existingDevice = await _context.UserDevices
            .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceFingerprint == deviceFingerprint);

        if (existingDevice != null)
        {
            // 更新现有设备
            existingDevice.LastUsedAt = DateTime.UtcNow;
            existingDevice.LastIpAddress = ipAddress;
            existingDevice.LastLocation = location;
            existingDevice.UsageCount++;

            // 更新设备信息（可能有变化）
            existingDevice.Browser = deviceInfo.Browser;
            existingDevice.BrowserVersion = deviceInfo.BrowserVersion;
            existingDevice.OperatingSystem = deviceInfo.OperatingSystem;
            existingDevice.OsVersion = deviceInfo.OsVersion;
            existingDevice.ScreenResolution = deviceInfo.ScreenResolution;
            existingDevice.TimeZone = deviceInfo.TimeZone;
            existingDevice.Language = deviceInfo.Language;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated device {DeviceId} for user {UserId}", existingDevice.Id, userId);
            return existingDevice;
        }

        // 创建新设备
        var newDevice = new UserDevice
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceFingerprint = deviceFingerprint,
            DeviceName = GenerateDeviceName(deviceInfo),
            Browser = deviceInfo.Browser,
            BrowserVersion = deviceInfo.BrowserVersion,
            OperatingSystem = deviceInfo.OperatingSystem,
            OsVersion = deviceInfo.OsVersion,
            DeviceType = deviceInfo.DeviceType,
            ScreenResolution = deviceInfo.ScreenResolution,
            TimeZone = deviceInfo.TimeZone,
            Language = deviceInfo.Language,
            Platform = deviceInfo.Platform,
            FirstUsedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow,
            LastIpAddress = ipAddress,
            LastLocation = location,
            IsTrusted = false, // 新设备默认不信任
            IsActive = true,
            UsageCount = 1
        };

        _context.UserDevices.Add(newDevice);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Registered new device {DeviceId} for user {UserId}", newDevice.Id, userId);
        return newDevice;
    }

    public async Task<List<UserDevice>> GetUserDevicesAsync(Guid userId)
    {
        return await _context.UserDevices
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.LastUsedAt)
            .ToListAsync();
    }

    public async Task<UserDevice?> GetDeviceAsync(Guid deviceId)
    {
        return await _context.UserDevices.FindAsync(deviceId);
    }

    public async Task<bool> IsKnownDeviceAsync(Guid userId, string deviceFingerprint)
    {
        return await _context.UserDevices
            .AnyAsync(d => d.UserId == userId && d.DeviceFingerprint == deviceFingerprint);
    }

    public async Task TrustDeviceAsync(Guid deviceId, bool trusted = true)
    {
        var device = await _context.UserDevices.FindAsync(deviceId);
        if (device == null)
        {
            throw new InvalidOperationException($"Device {deviceId} not found");
        }

        device.IsTrusted = trusted;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Device {DeviceId} trust status changed to {Trusted}", deviceId, trusted);
    }

    public async Task SetDeviceActiveAsync(Guid deviceId, bool active)
    {
        var device = await _context.UserDevices.FindAsync(deviceId);
        if (device == null)
        {
            throw new InvalidOperationException($"Device {deviceId} not found");
        }

        device.IsActive = active;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Device {DeviceId} active status changed to {Active}", deviceId, active);
    }

    public async Task DeleteDeviceAsync(Guid deviceId)
    {
        var device = await _context.UserDevices.FindAsync(deviceId);
        if (device == null)
        {
            throw new InvalidOperationException($"Device {deviceId} not found");
        }

        _context.UserDevices.Remove(device);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Device {DeviceId} deleted", deviceId);
    }

    public async Task RenameDeviceAsync(Guid deviceId, string newName)
    {
        var device = await _context.UserDevices.FindAsync(deviceId);
        if (device == null)
        {
            throw new InvalidOperationException($"Device {deviceId} not found");
        }

        device.DeviceName = newName;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Device {DeviceId} renamed to {NewName}", deviceId, newName);
    }

    public async Task<DeviceStatistics> GetDeviceStatisticsAsync(Guid userId)
    {
        var devices = await _context.UserDevices
            .Where(d => d.UserId == userId)
            .ToListAsync();

        var stats = new DeviceStatistics
        {
            TotalDevices = devices.Count,
            TrustedDevices = devices.Count(d => d.IsTrusted),
            ActiveDevices = devices.Count(d => d.IsActive),
            LastDeviceAdded = devices.Any() ? devices.Max(d => d.FirstUsedAt) : null,
            DeviceTypeCounts = devices
                .GroupBy(d => d.DeviceType ?? "Unknown")
                .Select(g => new DeviceTypeCount
                {
                    DeviceType = g.Key,
                    Count = g.Count()
                })
                .ToList()
        };

        return stats;
    }

    private string GenerateDeviceName(DeviceInfo deviceInfo)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(deviceInfo.DeviceType))
        {
            parts.Add(deviceInfo.DeviceType);
        }

        if (!string.IsNullOrEmpty(deviceInfo.Browser))
        {
            parts.Add(deviceInfo.Browser);
        }

        if (!string.IsNullOrEmpty(deviceInfo.OperatingSystem))
        {
            parts.Add(deviceInfo.OperatingSystem);
        }

        return parts.Count > 0 ? string.Join(" - ", parts) : "Unknown Device";
    }
}

