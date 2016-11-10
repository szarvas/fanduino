(*
Copyright 2016 Attila Szarvas

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*)

module Maybe

let bind f xT =
    match xT with
    | Some x -> f x
    | None -> None

let ret x = Some x

type MaybeBuilder() =
    member this.Return x = ret x
    member this.ReturnFrom x = x
    member this.Bind(x,f) = bind f x
    member this.Zero() = None

let maybe = new MaybeBuilder()
