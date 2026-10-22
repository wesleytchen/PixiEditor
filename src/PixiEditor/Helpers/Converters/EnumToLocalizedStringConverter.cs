using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using PixiEditor.UI.Common.Localization;

namespace PixiEditor.Helpers.Converters;

internal class EnumToLocalizedStringConverter : SingleInstanceConverter<EnumToLocalizedStringConverter>
{
    private Dictionary<object, string> enumTranslations = new(
        typeof(EnumToLocalizedStringConverter).Assembly
            .GetCustomAttributes()
            .OfType<ILocalizeEnumInfo>()
            .Select(x => new KeyValuePair<object, string>(x.GetEnumValue(), x.LocalizationKey)));
    
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Enum enumValue)
        {
            return value;
        }

        if (enumTranslations.TryGetValue(enumValue, out var assemblyDefinedKey))
        {
            return assemblyDefinedKey;
        }

        if (EnumHelpers.HasDescription(enumValue))
        {
            return EnumHelpers.GetDescription(enumValue);
        }

        if (enumValue.GetType().Namespace ==
            "PixiEditor.ChangeableDocument.Changeables.Graph.Nodes.Effects" &&
            (enumValue.GetType().Name.EndsWith("Effect") ||
             enumValue.GetType().Name is "AsciiMagicPalette" or "AsciiMagicChroma"))
        {
            return HumanizeAsciiMagicEffect(enumValue.ToString());
        }

        ThrowUntranslatedEnumValue(enumValue);
        return enumValue;
    }

    private static string HumanizeAsciiMagicEffect(string name)
    {
        if (name == "Anaglyph3D") return "Anaglyph 3D";
        if (name == "Lofi") return "Lo-fi";
        if (name == "OriginalColors") return "Original colors";
        if (name == "Mono1Bit") return "Mono 1-bit";
        if (name == "Gray4Levels") return "Gray 2-bit (4 levels)";
        if (name == "Gray8Levels") return "Gray 3-bit (8 levels)";
        if (name == "CGAPalette0") return "CGA palette 0";
        if (name == "CGAPalette1") return "CGA palette 1";
        if (name == "Pico8") return "PICO-8";
        if (name == "GameBoy") return "Game Boy";
        if (name == "PerChannelRGB") return "Per-channel RGB";

        string withAcronymsSplit = Regex.Replace(name, "([A-Z])([A-Z][a-z])", "$1 $2");
        return Regex.Replace(withAcronymsSplit, "([a-z])([A-Z])", "$1 $2");
    }

    [Conditional("DEBUG")]
    private static void ThrowUntranslatedEnumValue(object value)
    {
        throw new ArgumentException(
            $"Enum value '{value.GetType()}.{value}' has no value defined. Either add a Description attribute to the enum values or a LocalizeEnum attribute in EnumTranslations.cs for third party enums",
            nameof(value));
    }
}
