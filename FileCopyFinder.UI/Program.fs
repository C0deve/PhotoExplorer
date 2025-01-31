namespace CounterApp

open System.IO
open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Controls.Templates
open Avalonia.Media
open Avalonia.Themes.Fluent
open Avalonia.FuncUI.Hosts
open Avalonia.Controls
open Avalonia.Media.Imaging
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL

module Main =

    let view () =
        Component(fun ctx ->
            let drivers = ctx.useState Array.empty
            let firstDirectories: IWritable<string array> = ctx.useState Array.empty
            let secondDirectories: IWritable<string array> = ctx.useState Array.empty
            let fileInfoList: IWritable<string array> = ctx.useState Array.empty

            ctx.useEffect (
                handler =
                    (fun _ ->
                        let newDrives = DriveInfo.GetDrives()
                        drivers.Set newDrives),
                triggers = [ EffectTrigger.AfterInit ]
            )

            let driveInfoTemplate =
                DataTemplateView.create<_, DriveInfo> (fun data -> TextBlock.create [ TextBlock.text data.Name ])

            let directoryOrFileTemplate =
                DataTemplateView.create<_, string> (fun (data: obj) ->
                    let name =
                        match data with
                        | :? string as strData -> Path.GetFileName strData
                        | _ -> ""

                    TextBlock.create [ TextBlock.text name ])

            let imageTemplate =
                DataTemplateView.create<_, string> (fun (data: obj) ->
                    let imageWidth = 200
                    let imageControl =
                        match data with
                        | :? string as strData ->
                            let stream = File.OpenRead strData
                            let image = Bitmap.DecodeToWidth(stream, imageWidth, BitmapInterpolationMode.LowQuality)

                            Image.create
                                [ Image.width imageWidth
                                  Image.height imageWidth
                                  Image.source image
                                  Image.stretch Stretch.Uniform ]
                        | _ -> Image.create [ Image.width imageWidth; Image.height imageWidth ]
                    
                    Border.create  [
                        Border.cornerRadius 10
                        Border.clipToBounds true
                        Border.child imageControl
                    ])

            let setFileList path =
                fileInfoList.Set(Directory.GetFiles(path, "*.png"))

            let setChildren path (state: IWritable<string array>) =
                printfn $"{path}"
                state.Set(Directory.GetDirectories path)
                setFileList path

            let onDriveChanged (driver: obj) =
                match driver with
                | :? DriveInfo as driveInfo -> setChildren driveInfo.Name firstDirectories
                | _ -> ()

            let onFirstDirectoryChanged (path: obj) =
                match path with
                | :? string as directoryPath -> setChildren directoryPath secondDirectories
                | _ -> ()

            let onSecondDirectoryChanged (path: obj) =
                match path with
                | :? string as directoryPath -> setFileList directoryPath
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
                        listBox firstDirectories directoryOrFileTemplate onFirstDirectoryChanged
                        listBox secondDirectories directoryOrFileTemplate onSecondDirectoryChanged
                        ListBox.create
                            [ ListBox.dock Dock.Right
                              ListBox.dataItems fileInfoList.Current
                              ListBox.itemTemplate directoryOrFileTemplate ]
                        ListBox.create
                            [ ListBox.itemsPanel (FuncTemplate<Panel>(fun _ -> WrapPanel()))
                              ListBox.itemTemplate imageTemplate
                              ListBox.dataItems fileInfoList.Current
                              ListBox.dock Dock.Right ] ] ])

type MainWindow() =
    inherit HostWindow()

    do
        base.Title <- "Photos :)"
        base.Content <- Main.view ()

type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Add(FluentTheme())
        this.RequestedThemeVariant <- Styling.ThemeVariant.Dark

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime -> desktopLifetime.MainWindow <- MainWindow()
        | _ -> ()

module Program =

    [<EntryPoint>]
    let main (args: string[]) =
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .StartWithClassicDesktopLifetime(args)
