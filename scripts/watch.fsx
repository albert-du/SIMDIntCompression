// Continuously check for changes to to ./scripts/generateIntegrated.fsx and run the script when it changes

let name = "generateIntegrated.fsx"
let script = __SOURCE_DIRECTORY__ + "/" + name

let target =
    __SOURCE_DIRECTORY__ + "/../SIMDIntCompression/SimdBitPacking32D4.Simd.cs"

// runs the script
let run () =
    try
        use p = new System.Diagnostics.Process()
        p.StartInfo.FileName <- "dotnet"
        p.StartInfo.Arguments <- $"fsi {script}"
        p.StartInfo.UseShellExecute <- false
        p.StartInfo.RedirectStandardOutput <- true
        p.Start() |> ignore
        System.IO.File.WriteAllText(target, p.StandardOutput.ReadToEnd())
        p.WaitForExit()


        printfn "Generated %s\n" target
    with e ->
        printfn "Error: %s\n" e.Message

// run it once

run ()

// watch for changes

let watcher = new System.IO.FileSystemWatcher __SOURCE_DIRECTORY__
watcher.Filter <- name
watcher.EnableRaisingEvents <- true
watcher.Changed.Add(fun _ -> printf "Change detected:"; run())

printfn "Watching for changes to %s" script

while true do
    System.Threading.Thread.Sleep 1000
