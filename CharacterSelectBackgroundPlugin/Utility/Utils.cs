using System;
using System.Linq;

namespace CharacterSelectBackgroundPlugin.Utility
{
    public static class Utils
    {
        public static nint GetStaticAddressFromSigOrThrow(string signature, int offset = 0)
        {
            if (Services.SigScanner.TryGetStaticAddressFromSig(signature, out var result, offset))
            {
                return result;
            }
            else
            {
                throw new Exception($"Failed to get static address from '{signature}'");
            }
        }

        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value[..Math.Min(value.Length, maxLength)];
        }

        public static string ToText(this Enum value)
        {
            return value.GetType()?
                .GetField(value.ToString())?
                .GetCustomAttributes(typeof(EnumTranslationAttribute), false)
                .SingleOrDefault() is not EnumTranslationAttribute attribute ? value.ToString() : attribute.Translation;
        }

        public static float NormalizeAngle(float angle)
        {
            var normalized = angle % (Math.PI * 2);
            if (normalized <= -Math.PI)
                normalized += Math.PI * 2;
            else if (normalized > Math.PI)
                normalized -= Math.PI * 2;
            return (float)normalized;
        }
    }
}
