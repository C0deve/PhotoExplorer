#load "Core.fs"

open System.IO

let files = FileCopyFinder.Core.getFilesGroupByName "D:\\Veranda\\Tmp"

files |> FileCopyFinder.Core.findCopy |> FileCopyFinder.Core.print

let drives = DriveInfo.GetDrives()
