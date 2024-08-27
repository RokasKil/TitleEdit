using Dalamud.Utility;
using System;
using System.IO;
using System.Linq;
using System.Numerics;

namespace TitleEdit.Utility
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
            var normalized = angle % (MathF.PI * 2);
            if (normalized <= -MathF.PI)
                normalized += MathF.PI * 2;
            else if (normalized > MathF.PI)
                normalized -= MathF.PI * 2;
            return normalized;
        }

        public static Vector3 GetVectorFromAngles(float yaw, float pitch)
        {
            var xzLen = MathF.Cos(pitch);
            return new(xzLen * MathF.Sin(yaw), MathF.Sin(pitch), xzLen * MathF.Cos(yaw));
        }

        public static (float yaw, float pitch) GetAnglesFromVector(Vector3 vector)
        {
            vector = Vector3.Normalize(vector);
            var yaw = MathF.Atan2(vector.X, vector.Z);
            var pitch = MathF.Asin(vector.Y);
            return (yaw, pitch);
        }

        public static void IterateFiles(DirectoryInfo directory, Action<FileInfo, string> action, string path = "")
        {
            foreach (var file in directory.EnumerateFiles())
            {
                action(file, path);
            }
            foreach (var subDirectory in directory.EnumerateDirectories())
            {
                if (subDirectory.LinkTarget.IsNullOrEmpty())
                {
                    IterateFiles(subDirectory, action, Path.Join(path, subDirectory.Name));
                }
            }

        }
    }
}
