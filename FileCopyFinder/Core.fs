namespace FileCopyFinder

module Core =
    open System.IO

    let getFilesGroupByName (path: string) : Map<string,string list> =
        Directory.EnumerateFileSystemEntries(path, "*", SearchOption.AllDirectories)
        |> Seq.map (fun x ->
            {|  Name = Path.GetFileNameWithoutExtension x
                Path = x
            |})
        |> Seq.sortBy  _.Name
        |> Seq.groupBy  _.Name // Group by the name before the extension
        |> Seq.map (fun (key, group) -> 
            let pathList = group |> Seq.map _.Path |> Seq.toList
            key, pathList )
        |> Map.ofSeq
    
    let findCopy (inputFiles: Map<string, string list>) =
        let mapList = Map.toList inputFiles 
        ([], mapList)
        ||> List.fold (fun state (currentKey, currentFiles) ->
            match state with
            | [] -> [ (currentKey, currentFiles) ]
            | [ (key, files) ] when currentKey.StartsWith key -> [ (key, currentFiles @ files) ]
            | (key, files) :: tail when currentKey.StartsWith key -> (key, currentFiles @ files) :: tail
            | _ -> (currentKey, currentFiles) :: state)
        |> Map.ofList
            
    let print (groupedFiles:Map<string,string list>) =
        groupedFiles
        |> Map.iter (fun key files ->
            printfn $"%s{key} {files.Length} files"
            files
            |> List.iter (fun path ->
                let parent = Path.GetDirectoryName path
                printfn $"  {path}  {parent}"))
