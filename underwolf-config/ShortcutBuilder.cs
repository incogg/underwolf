using IWshRuntimeLibrary;

namespace underwolf_config {
    public static class ShortcutBuilder {

        /// <summary>
        /// Creates a shortcut and writes it to the disk
        /// </summary>
        /// <param name="path">location of the shortcut</param>
        /// <param name="target">target program</param>
        /// <param name="args">arguments to pass into the target program</param>
        /// <param name="iconLocation">location of an icon to use</param>
        /// <param name="description">description of the shortcut</param>
        public static void Build( string path, string target, string args = "", string iconLocation = "", string? description = "") {
            WshShell shell = new();
            IWshShortcut shortcut = shell.CreateShortcut(path);
            shortcut.IconLocation = iconLocation;
            shortcut.TargetPath = target;
            shortcut.Arguments = args;
            shortcut.Description = description;
            shortcut.Save();
        }
    }
}
