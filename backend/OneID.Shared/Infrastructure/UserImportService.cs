using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OneID.Shared.Domain;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace OneID.Shared.Infrastructure;

/// <summary>
/// 用户导入服务
/// </summary>
public interface IUserImportService
{
    Task<UserImportResult> ImportUsersFromCsvAsync(
        Stream csvStream, 
        string? defaultRole = null,
        string? importedBy = null,
        CancellationToken cancellationToken = default);
    
    string GenerateSampleCsv();
}

/// <summary>
/// 用户导入结果
/// </summary>
public class UserImportResult
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<UserImportError> Errors { get; set; } = new();
    public List<string> CreatedUserIds { get; set; } = new();
}

/// <summary>
/// 用户导入错误
/// </summary>
public class UserImportError
{
    public int RowNumber { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// CSV 用户记录
/// </summary>
public class CsvUserRecord
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Role { get; set; }
}

/// <summary>
/// 用户导入服务实现
/// </summary>
public class UserImportService : IUserImportService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<UserImportService> _logger;

    public UserImportService(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        IAuditLogService auditLogService,
        ILogger<UserImportService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<UserImportResult> ImportUsersFromCsvAsync(
        Stream csvStream,
        string? defaultRole = null,
        string? importedBy = null,
        CancellationToken cancellationToken = default)
    {
        var result = new UserImportResult();
        var rowNumber = 1; // 从 1 开始（跳过标题行）

        try
        {
            using var reader = new StreamReader(csvStream);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                BadDataFound = null // 忽略错误数据
            };

            using var csv = new CsvReader(reader, config);
            var records = csv.GetRecords<CsvUserRecord>();

            foreach (var record in records)
            {
                rowNumber++;
                result.TotalRows++;

                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("User import cancelled at row {RowNumber}", rowNumber);
                    break;
                }

                // 验证必填字段
                if (string.IsNullOrWhiteSpace(record.UserName) ||
                    string.IsNullOrWhiteSpace(record.Email) ||
                    string.IsNullOrWhiteSpace(record.Password))
                {
                    result.FailureCount++;
                    result.Errors.Add(new UserImportError
                    {
                        RowNumber = rowNumber,
                        UserName = record.UserName,
                        Email = record.Email,
                        ErrorMessage = "UserName, Email, and Password are required"
                    });
                    continue;
                }

                // 检查用户名是否已存在
                var existingUser = await _userManager.FindByNameAsync(record.UserName);
                if (existingUser != null)
                {
                    result.FailureCount++;
                    result.Errors.Add(new UserImportError
                    {
                        RowNumber = rowNumber,
                        UserName = record.UserName,
                        Email = record.Email,
                        ErrorMessage = $"Username '{record.UserName}' already exists"
                    });
                    continue;
                }

                // 检查邮箱是否已存在
                var existingEmail = await _userManager.FindByEmailAsync(record.Email);
                if (existingEmail != null)
                {
                    result.FailureCount++;
                    result.Errors.Add(new UserImportError
                    {
                        RowNumber = rowNumber,
                        UserName = record.UserName,
                        Email = record.Email,
                        ErrorMessage = $"Email '{record.Email}' already exists"
                    });
                    continue;
                }

                // 创建用户
                var user = new AppUser
                {
                    UserName = record.UserName,
                    Email = record.Email,
                    DisplayName = record.DisplayName ?? record.UserName,
                    EmailConfirmed = true, // 批量导入的用户默认已验证邮箱
                    IsExternal = false
                };

                var createResult = await _userManager.CreateAsync(user, record.Password);

                if (!createResult.Succeeded)
                {
                    result.FailureCount++;
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    result.Errors.Add(new UserImportError
                    {
                        RowNumber = rowNumber,
                        UserName = record.UserName,
                        Email = record.Email,
                        ErrorMessage = errors
                    });
                    continue;
                }

                // 分配角色
                var roleToAssign = !string.IsNullOrWhiteSpace(record.Role) 
                    ? record.Role 
                    : defaultRole ?? "User";

                var roleExists = await _roleManager.RoleExistsAsync(roleToAssign);
                if (roleExists)
                {
                    await _userManager.AddToRoleAsync(user, roleToAssign);
                }
                else
                {
                    _logger.LogWarning("Role '{Role}' does not exist, user created without role", roleToAssign);
                }

                result.SuccessCount++;
                result.CreatedUserIds.Add(user.Id.ToString());

                _logger.LogInformation(
                    "User imported: {UserName} ({Email}) at row {RowNumber}",
                    user.UserName, user.Email, rowNumber);
            }

            // 记录审计日志
            await _auditLogService.LogAsync(
                action: "Bulk User Import",
                category: "User",
                success: true,
                details: $"Imported {result.SuccessCount} users, {result.FailureCount} failures",
                userId: null,
                userName: importedBy
            );

            _logger.LogInformation(
                "User import completed: {Total} total, {Success} success, {Failure} failures",
                result.TotalRows, result.SuccessCount, result.FailureCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user import at row {RowNumber}", rowNumber);
            
            await _auditLogService.LogAsync(
                action: "Bulk User Import Failed",
                category: "User",
                success: false,
                errorMessage: ex.Message,
                userName: importedBy
            );

            throw;
        }
    }

    public string GenerateSampleCsv()
    {
        var csv = new StringBuilder();
        csv.AppendLine("UserName,Email,Password,DisplayName,Role");
        csv.AppendLine("john.doe,john.doe@example.com,Password123!,John Doe,User");
        csv.AppendLine("jane.smith,jane.smith@example.com,Password123!,Jane Smith,User");
        csv.AppendLine("admin.user,admin@example.com,AdminPass123!,Admin User,Admin");
        
        return csv.ToString();
    }
}
