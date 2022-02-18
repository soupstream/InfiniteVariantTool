using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace InfiniteVariantTool.GUI
{

    public abstract class IOService
    {
        public abstract void ShowInExplorer(string path);
        public abstract string? SelectPath(PathSelectType selectType, string? title = null, string? filter = null, string? initialPath = null);
        public bool TrySelectPath([NotNullWhen(true)] out string? path, PathSelectType selectType, string? title = null, string? filter = null, string? initialPath = null)
        {
            string? result = SelectPath(selectType, title, filter, initialPath);
            if (result == null)
            {
                path = null;
                return false;
            }
            else
            {
                path = result;
                return true;
            }
        }
    }

    public enum PathSelectType
    {
        Open,
        Save,
    }

    public class IOServiceImpl : IOService
    {
        private Window window;
        public IOServiceImpl(Window window)
        {
            this.window = window;
        }

        public override void ShowInExplorer(string path)
        {
            if (File.Exists(path))
            {
                Process.Start("explorer.exe", $"/select,\"{path}\"");
            }
            else
            {
                Process.Start("explorer.exe", $"\"{path}\"");
            }
        }

        public override string? SelectPath(PathSelectType selectType, string? title = null, string? filter = null, string? initialPath = null)
        {
            if (filter != null && filter.StartsWith("Directory"))
            {
                VistaFolderBrowserDialog dialog = new()
                {
                    UseDescriptionForTitle = true,
                    ShowNewFolderButton = true,
                };
                if (title != null)
                {
                    dialog.Description = title;
                }
                if (initialPath != null)
                {
                    dialog.SelectedPath = initialPath;
                }
                if (dialog.ShowDialog(window) == true)
                {
                    return dialog.SelectedPath;
                }
            }
            else
            {
                VistaFileDialog dialog = selectType switch
                {
                    PathSelectType.Open => new VistaOpenFileDialog(),
                    PathSelectType.Save => new VistaSaveFileDialog()
                    {
                        OverwritePrompt = false
                    },
                    _ => throw new ArgumentException()
                };
                if (filter != null)
                {
                    dialog.Filter = filter;
                }
                if (title != null)
                {
                    dialog.Title = title;
                }
                if (initialPath != null)
                {
                    dialog.InitialDirectory = initialPath;
                }
                if (dialog.ShowDialog(window) == true)
                {
                    return dialog.FileName;
                }
            }
            return null;
        }
    }
}
