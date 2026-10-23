namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Identifies the explicitly selected Banco Central Pix key format.</summary>
public enum PixKeyTypeDto
{
    /// <summary>No key type selected.</summary>
    None = 0,
    /// <summary>CPF taxpayer identifier.</summary>
    Cpf = 1,
    /// <summary>CNPJ taxpayer identifier.</summary>
    Cnpj = 2,
    /// <summary>International phone number.</summary>
    Phone = 3,
    /// <summary>Canonical email key.</summary>
    Email = 4,
    /// <summary>Random UUID key.</summary>
    Evp = 5
}
