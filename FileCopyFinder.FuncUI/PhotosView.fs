module FileCopyFinder.UI.PhotosView
    open System.IO
    open Avalonia.Controls.Templates
    open Avalonia.Media
    open Avalonia.Media.Imaging
    open Avalonia.Controls
    open Avalonia.FuncUI
    open Avalonia.FuncUI.DSL
    open Avalonia.FuncUI.Types
    
    let imageView stream =
        let imageWidth = 200             
        let image = Bitmap.DecodeToWidth(stream, imageWidth, BitmapInterpolationMode.LowQuality)
        
        Border.create [ Border.cornerRadius 10
                        Border.clipToBounds true
                        Border.child
                            (Image.create
                                [ Image.width imageWidth
                                  Image.height imageWidth
                                  Image.source image
                                  Image.stretch Stretch.Uniform ])]   

    let driveView onSelectionChanged =
        Component.create("drive-view", fun ctx ->
            let store: IWritable<DriveInfo array> = ctx.useState Array.empty
            ctx.useEffect (
                handler = (fun _ -> DriveInfo.GetDrives() |> store.Set),
                triggers = [ EffectTrigger.AfterInit ]
            )

            let template =
                DataTemplateView<DriveInfo>.create (fun data -> TextBlock.create [ TextBlock.text data.Name ])

            ListBox.create
                [ ListBox.dock Dock.Left
                  ListBox.dataItems store.Current
                  ListBox.itemTemplate template
                  ListBox.onSelectedItemChanged onSelectionChanged ] :> IView)
    
    let directoriesView (parent : IReadable<string option>) onSelectionChanged =
        Component.create($"directories-view", fun ctx ->
            let parent = ctx.usePassedRead parent
            let store: IWritable<string array> = ctx.useState Array.empty
            ctx.useEffect (
                handler = (fun _ ->
                   printfn $"{parent.Current}"
                   store.Set Array.empty
                   match parent.Current with
                   | Some path -> Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly)
                   | None -> Array.empty
                   |> store.Set),
                triggers = [ EffectTrigger.AfterChange parent ]
            )

            let template =
                DataTemplateView<string>.create (fun data ->
                    let name = Path.GetFileName data
                    TextBlock.create [ TextBlock.text name ])

            ListBox.create
                [ ListBox.dock Dock.Left
                  ListBox.dataItems store.Current
                  ListBox.itemTemplate template
                  ListBox.onSelectedItemChanged onSelectionChanged ] :> IView)

    let imageListView (store : IReadable<string array>) =
        Component.create("image-list", fun ctx ->
            let imageTemplate data = imageView (File.OpenRead data)
            let store = ctx.usePassedRead store
            ListBox.create
                            [ ListBox.itemsPanel (FuncTemplate<Panel>(fun _ -> WrapPanel()))
                              ListBox.itemTemplate (DataTemplateView<string>.create imageTemplate)
                              ListBox.dataItems store.Current
                              ListBox.dock Dock.Right ]
            )   
        
    let view () =
        Component(fun ctx ->
            let drive: IWritable<DriveInfo option> = ctx.useState None
            let firstLevel: IWritable<string option> = ctx.useState None
            let secondLevel: IWritable<string option> = ctx.useState None
            let fileInfoList: IWritable<string array> = ctx.useState Array.empty
            let driveName = drive.Map (fun driveInfoOption ->
                driveInfoOption
                |> Option.map _.Name)           
            
            let setFileList path =
                fileInfoList.Set(Directory.GetFiles(path, "*.png"))
                                       
            drive.Current |> Option.map _.Name |> Option.iter(setFileList) 
            firstLevel.Current |>  Option.iter(setFileList) 
            secondLevel.Current |> Option.iter(setFileList) 
            
            let fileTemplate =
                DataTemplateView<string>.create (fun data ->
                    let name = Path.GetFileName data
                    TextBlock.create [ TextBlock.text name ])
           
            let onDriveChanged (aDrive: obj) =
                aDrive :?> DriveInfo |> Option.ofObj |> drive.Set
                
            let onFirstDirectoryChanged (path: obj) =
                path :?> string |> Option.ofObj |> firstLevel.Set

            let onSecondDirectoryChanged (path: obj) =
                path :?> string |> Option.ofObj |> secondLevel.Set
            
            DockPanel.create
                [ DockPanel.children
                      [ driveView onDriveChanged
                        directoriesView driveName onFirstDirectoryChanged
                        directoriesView firstLevel onSecondDirectoryChanged
                        
                        ListBox.create
                            [ ListBox.dock Dock.Right
                              ListBox.dataItems fileInfoList.Current
                              ListBox.itemTemplate fileTemplate ]
                        imageListView fileInfoList ] ])
