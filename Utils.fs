module Utils

let trim (input : string) =
    input.Trim()

let replace (oldValue : string) (newValue : string) (input : string ) =
    input.Replace(oldValue, newValue)

let split (str : string) (separator : char) =
    str.Split separator
