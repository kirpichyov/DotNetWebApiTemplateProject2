using SampleProject.Core.Delegates;

namespace SampleProject.DataAccess.EntityConfigurations;

public static class Common
{
    public static TEnum ToEnumFast<TEnum>(string value, EnumTryParseDelegate<TEnum> converter)
        where TEnum : Enum
    {
        var isConverted = converter(value, out var result);
        if (!isConverted)
        {
            throw new InvalidOperationException($"Cannot convert value '{value}' to {typeof(TEnum).Name}");
        }
        
        return result;
    }
}