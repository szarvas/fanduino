module Result

type Result<'a, 'b> =
    | Success of 'a
    | Failure of 'b list
 
let ret x = Success x

let bind f xT =
    match xT with
    | Success x -> f x
    | Failure errs -> Failure errs

let map f xT =
    match xT with
    | Success x -> Success (f x)
    | Failure errs -> Failure errs

let apply fT xT =
    match fT, xT with
    | Success f, Success x -> Success (f x)
    | Success f, Failure errs -> Failure errs
    | Failure errs, Success x -> Failure errs
    | Failure errs1, Failure errs2 -> Failure (List.concat [errs1; errs2])

let (<!>) = map
let (<*>) = apply
let (>>=) x f = bind f x

type ResultBuilder() =
    member this.Return x = ret x
    member this.Bind(x,f) = bind f x
    member this.ReturnFrom x = x
    member this.Zero() = Failure []

let result = new ResultBuilder()
