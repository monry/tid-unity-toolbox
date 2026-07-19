using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace Tid.Toolbox.Editor
{
    public static class ToolboxEditorUtility
    {
        private static Dictionary<Type, StyleSheet> CachedStyleSheets { get; } = new();

        public static StyleSheet LoadStyleSheet<T>() =>
            CachedStyleSheets.TryGetValue(typeof(T), out var styleSheet)
                ? styleSheet
                : CachedStyleSheets[typeof(T)] =
                    AssetDatabase
                        .FindAssets($"t:stylesheet {typeof(T).Name}", IsPackaged ? new[] { $"Packages/{Constants.PackageId}", } : new[] { "Assets", })
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Select(AssetDatabase.LoadAssetAtPath<StyleSheet>)
                        .FirstOrDefault()
                    ?? throw new InvalidOperationException($"Could not find stylesheet for {typeof(T).Name}");

        private static bool IsPackaged => AssetDatabase.IsValidFolder($"Packages/{Constants.PackageId}");
    }
}