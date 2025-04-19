using Melissa.Core.AiTools.Holidays;
using Microsoft.EntityFrameworkCore;

namespace Melissa.Core.ExternalData;

public class AppDbContext : DbContext
{
    public DbSet<Holiday> Holidays { get; set; }

    private string DbPath { get; }
    
    public AppDbContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = Path.Join(path, "melissa.db");
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");
}