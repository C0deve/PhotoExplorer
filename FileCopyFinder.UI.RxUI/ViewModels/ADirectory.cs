using System.IO;

namespace FileCopyFinder.UI.RxUI.ViewModels;

public record ADirectory(string FullPath)
{
    public string Name => Path.GetFileName(FullPath);
}