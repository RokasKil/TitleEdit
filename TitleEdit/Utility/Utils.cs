using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.IO;
using System.Linq;
using System.Numerics;

namespace TitleEdit.Utility
{
    public static class Utils
    {
        public static readonly float RadToDegreeRatio = 180f / MathF.PI;
        public static readonly float DegreeToRadRatio = MathF.PI / 180f;

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

        public static string? ToTooltip(this Enum value)
        {
            return value.GetType()?
                .GetField(value.ToString())?
                .GetCustomAttributes(typeof(EnumTranslationAttribute), false)
                .SingleOrDefault() is not EnumTranslationAttribute attribute ? null : attribute.Tooltip;
        }

        public static bool IsInAvailableExpansion(this Enum value)
        {
            return value.GetType()?
                .GetField(value.ToString())?
                .GetCustomAttributes(typeof(EnumExpansionAttribute), false)
                .SingleOrDefault() is not EnumExpansionAttribute attribute ? true : Services.ExpansionService.HasExpansion(attribute.expansion);
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

        // the % is a reminder operator not modulo (-1 mod 9 should be 8 not -1) 
        public static int Modulo(int k, int n)
        {
            return ((k %= n) < 0) ? k + n : k;
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

        public unsafe delegate void NodeAction(AtkResNode* node);

        //I realized I could use the addon's UldManager but don't wanna refactor now
        public static unsafe void IterateNodes(AtkResNode* node, NodeAction action, bool iterateSiblings = true)
        {
            action(node);
            if (node->ChildNode != null)
            {
                IterateNodes(node->ChildNode, action);
            }
            if (iterateSiblings) // logic yoinked from dalamud cause wtf is this
            {
                var prevNode = node;
                while ((prevNode = prevNode->PrevSiblingNode) != null)
                    IterateNodes(prevNode, action, false);

                var nextNode = node;
                while ((nextNode = nextNode->NextSiblingNode) != null)
                    IterateNodes(nextNode, action, false);
            }
            if ((int)node->Type >= 1000)
            {
                var compNode = (AtkComponentNode*)node;
                if (compNode->Component != null)
                {
                    for (int i = 0; i < compNode->Component->UldManager.NodeListCount; i++)
                    {
                        // might not work with sub components if that exists
                        action(compNode->Component->UldManager.NodeList[i]);
                    }
                }

            }
        }
    }
}
