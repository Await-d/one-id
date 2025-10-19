namespace OneID.Shared.Domain;

/// <summary>
/// JWT签名密钥
/// </summary>
public class SigningKey
{
    /// <summary>
    /// 密钥ID（JWK kid）
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// 密钥类型（RSA, EC等）
    /// </summary>
    public string Type { get; set; } = "RSA";
    
    /// <summary>
    /// 密钥用途（签名、加密）
    /// </summary>
    public string Use { get; set; } = "sig"; // sig or enc
    
    /// <summary>
    /// 算法
    /// </summary>
    public string Algorithm { get; set; } = "RS256";
    
    /// <summary>
    /// 加密的私钥（PEM格式）
    /// </summary>
    public string EncryptedPrivateKey { get; set; } = string.Empty;
    
    /// <summary>
    /// 公钥（PEM格式）
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;
    
    /// <summary>
    /// 密钥创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// 密钥激活时间
    /// </summary>
    public DateTime? ActivatedAt { get; set; }
    
    /// <summary>
    /// 密钥过期时间
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// 密钥撤销时间
    /// </summary>
    public DateTime? RevokedAt { get; set; }
    
    /// <summary>
    /// 是否为当前活跃密钥
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// 密钥版本
    /// </summary>
    public int Version { get; set; }
    
    /// <summary>
    /// 备注
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// 租户ID（用于多租户）
    /// </summary>
    public Guid? TenantId { get; set; }
}

