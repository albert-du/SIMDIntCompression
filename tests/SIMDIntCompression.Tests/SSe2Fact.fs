namespace global

open Xunit

[<Sealed>]
type SSe2FactAttribute() as this =
    inherit FactAttribute()

    do
        if not System.Runtime.Intrinsics.X86.Sse2.IsSupported then
            this.Skip <- "Requires SSE2 support"