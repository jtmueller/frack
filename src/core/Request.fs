﻿namespace Frack
open Microsoft.Http

/// A helper type that parses an environment into more readily usable pieces.
type Request(env:Environment) =
  let input = match env?input with Inp(bs) -> bs |> Seq.toArray | _ -> Array.init 0 byte
  let request = new HttpRequestMessage(Content = HttpContent.Create(input))
  /// Creates a tuple from the first two values returned from a string split on the specified split character.
  let (|/) (split:char) (input:string) =
    if input |> isNotNullOrEmpty then
      let p = input.Split(split) in (p.[0], if p.Length > 1 then p.[1] else "")
    else ("","") // This should never be reached but has to be here to satisfy the return type.

  /// Tests a key value pair and returns Some(header, value) for true header values; otherwise None.
  let headers (KeyValue(header, value)) =
    let nonHeaders = ["HTTP_METHOD";"SCRIPT_NAME";"PATH_INFO";"QUERY_STRING";"url_scheme";"errors";"input";"version"]
    if nonHeaders |> Seq.exists ((=) header) then None else Some(header, read value)

  let urlFormEncoded =
    // Parse the input stream and the url-form-encoded values.
    seq { yield ("","") }

  // Parse the query string into a dictionary.
  let queryString =
    match env?QUERY_STRING with
    | Str(query) -> query.Split('&') |> Seq.filter isNotNullOrEmpty |> Seq.map ((|/) '&')
    | _          -> Seq.empty

  // Reconstruct the uri.
  let uri = System.Uri(
              (read env?url_scheme) + "://" +
              (read env?SERVER_NAME) +
              (match env?SERVER_PORT with Int(v) when v <> 80 -> ":" + v.ToString() | _ -> "") +
              (read env?SCRIPT_NAME) +
              (read env?PATH_INFO) +
              (match env?QUERY_STRING with Str(v) when isNotNullOrEmpty v -> "?" + v | _ -> ""))

  /// Gets a dictionary of headers
  member this.Headers = env |> Seq.choose headers |> dict

  member this.HttpMethod = read env?HTTP_METHOD

  /// Gets the dictionary of url-form-encoded values.
  member this.Post = dict urlFormEncoded

  /// Gets a dictionary containing the values from the query string and url-form-encoded values.
  member this.Params = seq { yield! queryString; yield! urlFormEncoded } |> dict

  /// Gets a dictionary of query string values.
  member this.Get = dict queryString

  /// Gets the requested uri.
  member this.Uri = uri