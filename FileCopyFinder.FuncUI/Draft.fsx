#load "Core.fs"
#r "nuget: MediaDevices, 1.10.0"
open System
open System.IO
open MediaDevices
let files = FileCopyFinder.Core.getFilesGroupByName "D:\\Veranda\\Tmp"

files |> FileCopyFinder.Core.findCopy |> FileCopyFinder.Core.print

let drives = DriveInfo.GetDrives()
let devices =
    MediaDevice.GetDevices()
    |> Seq.toArray

let phone = devices[1]
phone.Connect()
phone.FriendlyName
let f = phone.GetDirectoryInfo("\\")
f.EnumerateFiles("*.JPG", SearchOption.AllDirectories)
|> Seq.take 1
|> Seq.toArray
|> Seq.iter (fun x ->
    let exist = File.Exists($"Ce PC\Apple iPhone{x.FullName}")
    let dest = Path.Combine("D:","Veranda","tmp",x.Name)
    use stream = new MemoryStream()
    phone.DownloadFile(x.FullName, stream)
    File.WriteAllBytes(dest, stream.ToArray())
    printfn $"{x.Name} {x.FullName}: {exist}")

Environment.GetLogicalDrives()