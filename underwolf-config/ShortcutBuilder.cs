using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IWshRuntimeLibrary;

namespace underwolf_config {
    public static class ShortcutBuilder {

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
