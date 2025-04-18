namespace Melissa.Infraestructure.Holidays;

public class Holiday
{
    public DateTime Date { get; set; }
    public string Description { get; set; }
    public HolidayType Type { get; set; }
    public string? State { get; set; }
    public string? City { get; set; }
    public bool IsOptional { get; set; }
}

public enum HolidayType
{
    National,
    State,
    City
}