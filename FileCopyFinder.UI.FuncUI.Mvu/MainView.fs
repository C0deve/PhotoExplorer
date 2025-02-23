[<RequireQualifiedAccess>]
module MainView

open System.IO
open Elmish
open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Media.Imaging

type Model =
    { PhotoGallery: PhotoGallery.Model
      Drives: DriveInfo list
      SelectedDrive: DriveInfo option
      Directories: string list
      SelectedDirectory: string option
      SubDirectories: string list
      SelectedSubDirectory: string option }

type Msg =
    | PhotoGallery of PhotoGallery.Msg
    | LoadDrives
    | DriveSelected of DriveInfo
    | LoadDirectories of string
    | DirectorySelected of string
    | LoadSubDirectories of string
    | SubDirectorySelected of string


let private loadPhotoBitmap (path: string) =
    async {
        try
            use stream = File.OpenRead(path)
            let bitmap = Bitmap.DecodeToWidth(stream, 200, BitmapInterpolationMode.LowQuality)
            return Some(path, bitmap)
        with _ ->
            return None
    }

let private init () =
    { PhotoGallery = PhotoGallery.init ()
      Drives = []
      SelectedDrive = None
      Directories = []
      SelectedDirectory = None
      SubDirectories = []
      SelectedSubDirectory = None },
    Cmd.ofMsg LoadDrives

let private update msg model =

    match msg with
    | PhotoGallery msg ->
        let mPhotoGallery, cmd = PhotoGallery.update msg model.PhotoGallery

        { model with
            PhotoGallery = mPhotoGallery },
        Cmd.map PhotoGallery cmd

    | LoadDrives ->
        let drives = DriveInfo.GetDrives() |> Array.toList
        { model with Drives = drives }, Cmd.none

    | DriveSelected drive ->
        { model with
            SelectedDrive = Some drive
            Directories = []
            SelectedDirectory = None
            SubDirectories = []
            SelectedSubDirectory = None },
        Cmd.ofMsg (LoadDirectories drive.RootDirectory.FullName)

    | LoadDirectories path ->
        let dirs =
            try
                Directory.GetDirectories(path) |> Array.toList
            with _ ->
                []

        { model with Directories = dirs }, Cmd.none

    | DirectorySelected dir ->
        { model with
            SelectedDirectory = Some dir
            SubDirectories = []
            SelectedSubDirectory = None },
        Cmd.ofMsg (LoadSubDirectories dir)

    | LoadSubDirectories path ->
        let subdirs =
            try
                Directory.GetDirectories(path) |> Array.toList
            with _ ->
                []

        { model with SubDirectories = subdirs }, (PhotoGallery.Msg.StartLoadingPhotos path) |> PhotoGallery |> Cmd.ofMsg
    | SubDirectorySelected dir ->
        { model with
            SelectedSubDirectory = Some dir },
        (PhotoGallery.Msg.StartLoadingPhotos dir) |> PhotoGallery |> Cmd.ofMsg

let private view model dispatch =
    DockPanel.create
        [ DockPanel.children
              [ ListBox.create
                    [ ListBox.dock Dock.Left
                      ListBox.dataItems model.Drives
                      ListBox.itemTemplate (
                          DataTemplateView<DriveInfo>.create (fun drive ->
                              TextBlock.create [ TextBlock.text drive.Name ])
                      )
                      ListBox.onSelectedItemChanged (fun drive ->
                          match drive with
                          | null -> ()
                          | _ -> dispatch (DriveSelected(drive :?> DriveInfo))) ]
                ListBox.create
                    [ ListBox.dock Dock.Left
                      ListBox.dataItems model.Directories
                      ListBox.itemTemplate (
                          DataTemplateView<string>.create (fun dir ->
                              TextBlock.create [ TextBlock.text (Path.GetFileName dir) ])
                      )
                      ListBox.onSelectedItemChanged (fun dir ->
                          match dir with
                          | null -> ()
                          | _ -> dispatch (DirectorySelected(dir :?> string))) ]
                ListBox.create
                    [ ListBox.dock Dock.Left
                      ListBox.dataItems model.SubDirectories
                      ListBox.itemTemplate (
                          DataTemplateView<string>.create (fun dir ->
                              TextBlock.create [ TextBlock.text (Path.GetFileName dir) ])
                      )
                      ListBox.onSelectedItemChanged (fun dir ->
                          match dir with
                          | null -> ()
                          | _ -> dispatch (SubDirectorySelected(dir :?> string))) ]

                PhotoGallery.view model.PhotoGallery ] ]

let program = Program.mkProgram init update view
