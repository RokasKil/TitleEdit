using System;
using System.Numerics;

namespace CharacterSelectBackgroundPlugin.Utility
{
    public static class Utils
    {

        public static float[] ToArray(this Vector3 vector3) => [vector3.X, vector3.Y, vector3.Z];

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
    }
}
