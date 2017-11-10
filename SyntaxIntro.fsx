// Use let to assign variables.

let x = -3

// The "<-" operator is used to assign a value, however bindings are immutable by default
// so the following is a compile error.

x <- 2

// Mutability of a variable must explicitly declared. Use of mutable variables in F# is
// usually avoided unless absolutely necessary.

let mutable y = 5
y <- 2
y

// Almost everything in F# is an expression, rather than a statement like in languages like C#, Java, etc
// This means that an "if"" statement returns a value, so you can do this:

let z = if x > 0 then 2 else -2

// Use let to create an "add" function with parameters named x and y.  
let add x y = x + y
add 4 5

// F# is strongly typed and has powerful type inference. The parameters x and y of "add" were inferred to be integers
// Passing an invalid type to the function will not compile

add "34" 3


// Specifiying types explicitly

let addFloat (x: float) (y: float) : float = x + y
addFloat 3.4 2.3

// The pipe operator |> lets you pass the output of a function into another function.
// Consider we want to add, then multiply and subtract using the following functions.

let multiplyBy100 x = x * 100
let subtract35 x = x - 35

// One option is to do something like this, but this is somewhat verbose, you need to create and name multiple temporary variables

let added = add 5 9
let multiplied = multiplyBy100 added
let subtracted = subtract35 multiplied 
printfn "%i" subtracted

// This is concise but hard to read (unless you grok Lisp), you must read from the inside out

printfn "%i" (subtract35 (multiplyBy100 (add 5 9)))

// Using the pipe operator is concise and very readable

add 5 9
|> multiplyBy100
|> subtract35
|> printfn "%i"


// Lists

let listOfFloats = [1.3; 1.5; 3.4; 6.1; 9.3]
let listOfStrings = ["one"; "two"; "three"; "four"]

let max = 
    listOfFloats
    |> List.max
    |> printf "%f"

List.sum listOfFloats

// Passing a lambda function to List.map

List.map (fun (s : string) -> s.ToUpper()) listOfStrings

// or

listOfStrings
|> List.map (fun s -> s.ToUpper())

// For loops

for num in listOfFloats do
    printfn "%f" num


// Tuples

let tuple = 1, 2
let tuple2 = ("foo", 4)

// Records are immutable collections of named properties.

type Car = {
    Model: string;
    Make: string;
    MPG: float;
}

// Specifying the record type is optional, the type inference engine can figure out that this is a Car record

let vwR32 = {
    Model = "R32";
    Make ="Volkswagen";
    MPG = 21.;
}

let wrx : Car = {
    Model = "WRX";
    Make = "Subaru";
    MPG = 23.;
}

// You cannot modify a property of a record without using the mutable keyword, instead you can use the following
// "with" syntax to create a copy of an existing record with some of properties modified

let wrxSti = { wrx with Model = "WRX Sti"; MPG = 18. } 

// Classes

type MyClass() =
    let privateValue = "blah"
    member val Name = "" with get, set
    member this.AddToName(toAdd: string) = this.Name + toAdd


// Pattern matching. The match expression is sort of like a switch in other languages but is much more powerful.
// C# 7 may include first class language support for tuples, records, and pattern matching. 
// https://github.com/dotnet/roslyn/issues/2136
// https://www.kenneth-truyers.net/2016/01/20/new-features-in-c-sharp-7/

let printInteger x = 
    match x with
    | 0 -> printfn "zero"
    | 1 | 2 -> printfn "one or two"
    | x when x > 2 -> printfn "many"
    | x when x < 0 -> printfn "negative"
    | _ -> printfn "no way"

for i in -1..10 do
    printInteger i
    
// Matching on tuples

let matchTuple tuple = 
    match tuple with 
    | (0, 0) | (0, _) | (_, 0) -> printfn "Found a zero"
    | (1, x) -> printfn "one, %i" x
    | (x, 1) -> printfn "%i, one" x
    | (x, y) -> printfn "%i, %i" x y

matchTuple (0, 0)
matchTuple (1, 432)
matchTuple (12, 1)
matchTuple (2, 3)    

// Matching on lists

let matchList list =
    match list with
    | [] -> printfn "Empty"
    | 1 :: rest -> printfn "First value is one"
    | x :: y :: rest -> printfn "first = %i, second = %i" x y
    | _ -> printfn "something else"

matchList []
matchList [1; 2]
matchList [2]
matchList [2; 3; 5]

let matchR32 car = 
    match car with 
    | { Car.Model = model } when model = "R32" -> true
    | _ -> false

matchR32 vwR32
matchR32 { Model = "WRX"; Make = "Subaru"; MPG = 23. }
