namespace global

open System.Collections.Generic

[<AutoOpen>]
module Prelude =
    /// Generate a random posting list of the given size
    let generateRandomPosting size (next: unit -> uint32) =
        let values = HashSet()

        while values.Count < size do
            values.Add(next ()) |> ignore

        values |> Seq.sort |> Seq.toArray
