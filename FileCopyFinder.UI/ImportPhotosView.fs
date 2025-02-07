module FileCopyFinder.UI.ImportPhotosView

    open System.IO
    open Avalonia.Controls.Templates
    open Avalonia.Controls
    open Avalonia.FuncUI
    open Avalonia.FuncUI.DSL
    open MediaDevices
    open Avalonia.FuncUI.Types
    
    let importImageView () =
        Component(fun ctx ->
            let drivers = ctx.useState Array.empty
            let (currentDevice: IWritable<MediaDevice option>) = ctx.useState None
            let fileInfoList: IWritable<MediaFileInfo array> = ctx.useState Array.empty

            ctx.useEffect (
                handler =
                    (fun _ ->
                        let devices = MediaDevice.GetDevices() |> Seq.toArray
                        drivers.Set devices),
                triggers = [ EffectTrigger.AfterInit ]
            )

            let driveInfoTemplate =
                DataTemplateView.create<_, MediaDevice> (fun (data: obj) ->
                    let name =
                        match data with
                        | :? MediaDevice as strData -> strData.FriendlyName
                        | _ -> ""

                    TextBlock.create [ TextBlock.text name ])

            let imageTemplate =
                DataTemplateView<MediaFileInfo>.create (fun fileInfo ->
                    let stream =
                        match currentDevice.Current with
                        | Some device ->
                            let stream = new MemoryStream()
                            device.DownloadFile(fileInfo.FullName, stream)
                            stream.Position <- 0
                            Some stream
                        | None -> None

                    match stream with
                        | Some stream -> PhotosView.imageView stream :> IView
                        | None -> Rectangle.create([])
                    )

            let setFileList (path: MediaDirectoryInfo) =
                let files =
                    path.EnumerateFiles("*.JPG", SearchOption.AllDirectories)
                    |> Seq.take 10
                    |> Seq.toArray

                fileInfoList.Set(files)

            let setChildren (device: MediaDevice) =
                printfn $"{device.FriendlyName}"
                device.Connect()
                currentDevice.Set(Some device)
                setFileList (device.GetRootDirectory())

            let onDriveChanged (driver: obj) =
                match driver with
                | :? MediaDevice as driveInfo -> setChildren driveInfo
                | _ -> ()

            let listBox (items: IWritable<'t>) template onSelectionChanged =
                ListBox.create
                    [ ListBox.dock Dock.Left
                      ListBox.dataItems items.Current
                      ListBox.itemTemplate template
                      ListBox.onSelectedItemChanged onSelectionChanged ]

            DockPanel.create
                [ DockPanel.children
                      [ listBox drivers driveInfoTemplate onDriveChanged
                        ListBox.create
                            [ ListBox.itemsPanel (FuncTemplate<Panel>(fun _ -> WrapPanel()))
                              ListBox.itemTemplate imageTemplate
                              ListBox.dataItems fileInfoList.Current
                              ListBox.dock Dock.Right ] ] ])