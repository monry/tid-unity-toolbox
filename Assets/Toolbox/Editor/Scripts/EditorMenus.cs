using UnityEditor;
using UnityEditor.PackageManager;

namespace Tid.Toolbox.Editor;

public static class EditorMenus
{
    [MenuItem("TID/Toolbox/Embed Package")]
    private static void EmbedPackage()
    {
        Client.Embed("jp.ac.tid.toolbox");
    }
}