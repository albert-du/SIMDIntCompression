module UtilsTests

open System
open Xunit

open SIMDIntCompression

[<Fact>]
let ``SpanNonNegative`` () =
    Assert.True(Utils.SpanNonNegative([| 1; 2; 3; 4; 5 |].AsSpan()))
    Assert.False(Utils.SpanNonNegative([| 1; 2; -3; 4; 5 |].AsSpan()))
    Assert.False(Utils.SpanNonNegative([| 1; 2; 3; 4; -5 |].AsSpan()))
    Assert.False(Utils.SpanNonNegative([| 1; 2; -3; 4; -5 |].AsSpan()))

// longer tests
[<Fact>]
let ``SpanNonNegative longer`` () =
    let r = Random 1000

    let data = Array.init 10000 (fun _ -> r.Next(0, 1000))

    Assert.True(Utils.SpanNonNegative(data.AsSpan()))

    let data = Array.init 10000 (fun _ -> r.Next(-1000, 0))

    Assert.False(Utils.SpanNonNegative(data.AsSpan()))


// longer tests
[<Fact>]
let ``SpanNonNegative empty`` () =
    Assert.True(Utils.SpanNonNegative(Array.empty<int>.AsSpan ()))
