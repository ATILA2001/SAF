#nullable enable
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SAF.Data;

/// <summary>
/// Minimal DbContext that points to Auth.Web's database solely to share
/// the DataProtection key ring. Must not be used for any SAF data.
/// </summary>
public class DataProtectionDbContext(DbContextOptions<DataProtectionDbContext> options)
    : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
}
