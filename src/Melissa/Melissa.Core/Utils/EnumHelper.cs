using System.ComponentModel;
using System.Reflection;

namespace Melissa.Core.Utils;

public static class EnumHelper
{
    public static string GetEnumDescription(Enum value)
    {
        if (value == null)
            return string.Empty;
        
        FieldInfo field = value.GetType().GetField(value.ToString());
        DescriptionAttribute attribute = field.GetCustomAttribute<DescriptionAttribute>();
        return attribute == null ? value.ToString() : attribute.Description;
    }
}