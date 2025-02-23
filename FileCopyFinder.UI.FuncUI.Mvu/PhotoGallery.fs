[<RequireQualifiedAccess>]
module PhotoGallery

open System
open Avalonia.Controls.Templates
open Avalonia.Media
open System.IO
open Elmish
open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Media.Imaging

type LoadingPhotoId = Guid
type PhotoPath = string

type LoadOperation =
    { PhotosToLoad: PhotoPath list
      Id: LoadingPhotoId
      TotalCount: int }

type Model =
    { LoadedPhotos: (PhotoPath * Bitmap) list
      LoadOperation: LoadOperation option }

type Msg =
    | StartLoadingPhotos of PhotoPath
    | PhotosLoaded of (LoadingPhotoId * (PhotoPath * Bitmap) list)
    | TryToLoadNextPhotosIfAny of LoadingPhotoId
    | LoadingComplete of LoadingPhotoId

let parallelPhotoProcessingCount = 5

let private loadPhotoBitmap (path: PhotoPath) =
    async {
        try
            use stream = File.OpenRead(path)
            let bitmap = Bitmap.DecodeToWidth(stream, 200, BitmapInterpolationMode.LowQuality)
            return Some(path, bitmap)
        with _ ->
            return None
    }

let init () =
    { LoadedPhotos = []
      LoadOperation = None }

let update msg model =

    match msg, model.LoadOperation with
    | StartLoadingPhotos directory, _ ->
        let photosToLoad = Directory.EnumerateFiles(directory, "*.jpg") |> Seq.toList

        match photosToLoad with
        | [] -> init (), Cmd.none
        | photos ->
            let newOperationId = Guid.NewGuid()

            let operation =
                { Id = newOperationId
                  PhotosToLoad = photos
                  TotalCount = photos.Length }

            { model with
                LoadOperation = Some operation
                LoadedPhotos = [] },
            Cmd.ofMsg (TryToLoadNextPhotosIfAny newOperationId)

    | TryToLoadNextPhotosIfAny operationId, Some operation when operationId = operation.Id ->
        let nextPhoto = operation.PhotosToLoad |> List.truncate parallelPhotoProcessingCount

        let nextCmd =
            match nextPhoto with
            | [] -> Cmd.ofMsg (LoadingComplete operationId)
            | photos ->
                Cmd.OfAsync.perform
                    (fun paths ->
                        async {
                            let! results = paths |> List.map loadPhotoBitmap |> Async.Parallel
                            return operationId, results |> Array.choose id |> Array.toList
                        })
                    photos
                    PhotosLoaded

        model, nextCmd

    | PhotosLoaded(operationId, newPhotos), Some operation when operationId = operation.Id ->
        let remainingPhotos = operation.PhotosToLoad |> List.skip newPhotos.Length

        { model with
            LoadedPhotos = newPhotos @ model.LoadedPhotos
            LoadOperation =
                Some
                    { operation with
                        PhotosToLoad = remainingPhotos } },
        Cmd.ofMsg (TryToLoadNextPhotosIfAny operationId)

    | LoadingComplete(operationId), Some operation when operationId = operation.Id ->
        { model with LoadOperation = None }, Cmd.none

    | _ -> init (), Cmd.none

let private imageView source =
    let imageWidth = 200

    Border.create
        [ Border.cornerRadius 10
          Border.clipToBounds true
          Border.child (
              Image.create
                  [ Image.width imageWidth
                    Image.height imageWidth
                    Image.source source
                    Image.stretch Stretch.UniformToFill ]
          ) ]

let view model =
    DockPanel.create
        [ DockPanel.children
              [ match model.LoadOperation with
                | Some operation ->
                    ProgressBar.create
                        [ ProgressBar.dock Dock.Top
                          ProgressBar.margin (10.0, 0.0)
                          ProgressBar.minimum 0
                          ProgressBar.maximum (operation.TotalCount |> double)
                          ProgressBar.value model.LoadedPhotos.Length
                          ProgressBar.showProgressText true
                          ProgressBar.progressTextFormat "{}{0}/{3} Photos Complete ({1:0}%)" ]
                | None -> ()
                ListBox.create
                    [ ListBox.itemsPanel (FuncTemplate<Panel>(fun _ -> WrapPanel()))
                      ListBox.itemTemplate (DataTemplateView<Bitmap>.create imageView)
                      ListBox.dataItems (model.LoadedPhotos |> List.map snd)
                      ListBox.dock Dock.Right ] ] ]
