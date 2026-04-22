using POC.CustomerValidation.API.Models.DTOs;
using POC.CustomerValidation.API.Models.Entites;
using System.Text.RegularExpressions;

namespace POC.CustomerValidation.API.Extensions
{
    public static class DataMappingcs
    {
        public static string ToDigitsOnly(this string value)
            => Regex.Replace(value ?? string.Empty, @"\D", "");

        // Extension methods for mapping.
        public static void UpdateFromRequest(this FieldDefinition field, UpdateFieldDefinitionRequest request)
        {
            if (request.SectionId.HasValue)
                field.FieldSectionId = request.SectionId.Value;

            field.FieldLabel    = request.FieldLabel;
            field.FieldType     = request.FieldType;
            field.Placeholder   = request.PlaceholderText;
            field.HelpText      = request.HelpText;
            field.IsRequired    = request.IsRequired;
            field.IsActive      = request.IsActive;
            field.DisplayOrder  = request.DisplayOrder;
            field.MinValue      = request.MinValue;
            field.MaxValue      = request.MaxValue;
            field.MinLength     = request.MinLength;
            field.MaxLength     = request.MaxLength;
            field.RegexPattern  = request.RegexPattern;
            field.ModifiedDt    = DateTime.UtcNow;
        }


        public static void UpdateOptionFromRequest(this FieldOption option, UpdateFieldOptionRequest request)
        {
            option.OptionKey    = request.OptionKey;
            option.OptionLabel  = request.OptionLabel;
            option.DisplayOrder = request.DisplayOrder;
            option.IsActive     = request.IsActive;
        }
    }
}
