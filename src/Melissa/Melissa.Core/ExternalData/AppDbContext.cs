using Melissa.Core.AiTools.Holidays;
using Melissa.Core.AiTools.TaskList;
using Melissa.WebServer;
using Microsoft.EntityFrameworkCore;

namespace Melissa.Core.ExternalData;

public class AppDbContext : DbContext
{
    public DbSet<Holiday> Holidays { get; set; }
    public DbSet<DbConversationHistory> DbHistoryData { get; set; }
    public DbSet<Tasks> Tasks { get; set; }
    public DbSet<TaskItens> TaskItens { get; set; }

    private string DbPath { get; }
    
    public AppDbContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = Path.Join(path, "melissa.db");
        

        // Ensure the directory for the DB exists so SQLite can create the file.
        try
        {
            var dir = Path.GetDirectoryName(DbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
        catch
        {
            // If directory creation fails, let SQLite surface the error later. Avoid crashing here.
        }
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");
}