module VectorExtensionTests

#nowarn 44

open System.Runtime.Intrinsics
open System.Runtime.Intrinsics.X86
open Xunit

open SIMDIntCompression

[<Fact>]
let ``ShiftLeftLogical128BitLane`` () =
    
    let source = Vector128.Create(0uy,1uy,2uy,3uy,4uy,5uy,6uy,7uy,8uy,9uy,10uy,11uy,12uy,13uy,14uy,15uy)
    let expected = Vector128.Create(0uy, 0uy,1uy,2uy,3uy,4uy,5uy,6uy,7uy,8uy,9uy,10uy,11uy,12uy,13uy,14uy)

    let result = VectorExtensions.ShiftLeftLogical128BitLane(source, 1uy) 

    Assert.Equal(expected, result)


[<Fact>]
let ``ShiftRightLogical128BitLane`` () =
    
    let source = Vector128.Create(0uy,1uy,2uy,3uy,4uy,5uy,6uy,7uy,8uy,9uy,10uy,11uy,12uy,13uy,14uy,15uy)
    let expected = Vector128.Create(1uy,2uy,3uy,4uy,5uy,6uy,7uy,8uy,9uy,10uy,11uy,12uy,13uy,14uy,15uy,0uy)

    let result = VectorExtensions.ShiftRightLogical128BitLane(source, 1uy) 

    Assert.Equal(expected, result)


[<Fact>]
let ``Simulated ShiftLeftLogical128BitLane`` () =
    
    let source = Vector128.Create(0uy,1uy,2uy,3uy,4uy,5uy,6uy,7uy,8uy,9uy,10uy,11uy,12uy,13uy,14uy,15uy)
    let expected = Vector128.Create(0uy, 0uy,1uy,2uy,3uy,4uy,5uy,6uy,7uy,8uy,9uy,10uy,11uy,12uy,13uy,14uy)

    let result = VectorExtensions.DoNotCallDirectlyShiftLeftLogical128BitLaneAllPlatforms(source, 1uy) 

    Assert.Equal(expected, result)

[<Fact>]
let ``Simulated ShiftRightLogical128BitLane`` () =
    
    let source = Vector128.Create(0uy,1uy,2uy,3uy,4uy,5uy,6uy,7uy,8uy,9uy,10uy,11uy,12uy,13uy,14uy,15uy)
    let expected = Vector128.Create(1uy,2uy,3uy,4uy,5uy,6uy,7uy,8uy,9uy,10uy,11uy,12uy,13uy,14uy,15uy,0uy)

    let result = VectorExtensions.DoNotCallDirectlyShiftRightLogical128BitLaneAllPlatforms(source, 1uy) 

    Assert.Equal(expected, result)


[<SSe2Fact>]
let ``ShiftLeftLogical128BitLane matches SSe2 instruction`` () =
    let source = Vector128.Create(0uy,1uy,2uy,3uy,4uy,5uy,6uy,7uy,8uy,9uy,10uy,11uy,12uy,13uy,14uy,15uy)
    let expected = Sse2.ShiftLeftLogical128BitLane(source, 1uy)

    let result = VectorExtensions.DoNotCallDirectlyShiftLeftLogical128BitLaneAllPlatforms(source, 1uy) 

    Assert.Equal(expected, result)


[<SSe2Fact>]
let ``ShiftRightLogical128BitLane matches SSe2 instruction`` () =
    let source = Vector128.Create(0uy,1uy,2uy,3uy,4uy,5uy,6uy,7uy,8uy,9uy,10uy,11uy,12uy,13uy,14uy,15uy)
    let expected = Sse2.ShiftRightLogical128BitLane(source, 1uy)

    let result = VectorExtensions.DoNotCallDirectlyShiftRightLogical128BitLaneAllPlatforms(source, 1uy) 

    Assert.Equal(expected, result)


[<SSe2Fact>]
let ``ShiftLeftLogical128BitLane matches SSe2 instruction (2)`` () =
    let source = Vector128.Create(0uy,1uy,2uy,3uy,4uy,5uy,6uy,7uy,8uy,9uy,10uy,11uy,12uy,13uy,14uy,15uy)
    
    let mutable expected = source
    let mutable result = source

    // apply a bunch of shifts and check that the result is the same as the Sse2 instruction
    for _ in 0..15 do
        expected <- Sse2.ShiftLeftLogical128BitLane(expected, 1uy)
        result <- VectorExtensions.DoNotCallDirectlyShiftLeftLogical128BitLaneAllPlatforms(result, 1uy)

    Assert.Equal(expected, result)


[<SSe2Fact>]
let ``ShiftRightLogical128BitLane matches SSe2 instruction (2)`` () =
    let source = Vector128.Create(0uy,1uy,2uy,3uy,4uy,5uy,6uy,7uy,8uy,9uy,10uy,11uy,12uy,13uy,14uy,15uy)
    
    let mutable expected = source
    let mutable result = source

    // apply a bunch of shifts and check that the result is the same as the Sse2 instruction
    for _ in 0..15 do
        expected <- Sse2.ShiftRightLogical128BitLane(expected, 1uy)
        result <- VectorExtensions.DoNotCallDirectlyShiftRightLogical128BitLaneAllPlatforms(result, 1uy)

    Assert.Equal(expected, result)
