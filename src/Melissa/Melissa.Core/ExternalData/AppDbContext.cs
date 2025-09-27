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
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");
}