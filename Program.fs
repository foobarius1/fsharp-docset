open System.IO
open FSharp.Markdown
open System.Text.RegularExpressions
open SQLite
open Utils


let ContentPath = "FSharp.docset/Contents"
let ResourcePath = ContentPath + "/Resources"
let DocPath = ResourcePath + "/Documents"
let OrigDocPath = "visualfsharpdocs/docs/conceptual"

type EntryType =
    | Module
    | Function
    //| Keyword
    | Other // tmp

type Meta =
    { Title : string
      Description : string
      Assembly : string option
      Namespace : string option
      Type : EntryType
    }

let emptyMeta =
    { Title = ""
      Description = ""
      Assembly = None
      Namespace = None
      Type = Other
    }

[<CLIMutable>]
[<Table("searchIndex")>]
type Entry =
    { [<PrimaryKey; AutoIncrement>]
      [<Column("id")>]
      Id : int

      [<Column("name")>]
      Name : string

      [<Column("type")>]
      Type : string

      [<Column("path")>]
      Path : string }

let stripAndCleanDocLineForMeta (prefix : string) (line : string) =
    let re = Regex "\(in [\w\.]+dll\)"
    let l =
        (line.IndexOf prefix + String.length prefix)
        |> line.Substring
        |> String.filter (fun c ->
            not (c = '*' || c = ':'))
    re.Replace(l, "")
    |> trim

let fillMeta (meta : Meta) (line : string) =
    match line with
    | l when l.StartsWith "title:" ->
        let title = stripAndCleanDocLineForMeta "title:" l
        let typ =
            match title with
            | t when t.Contains "Module" -> Module
            | t when t.Contains "Function" -> Function
            | _ -> Other

        { meta with Title = title; Type = typ }

    | l when l.StartsWith "description:" ->
        { meta with Description = stripAndCleanDocLineForMeta "description:" l }

    | l when l.StartsWith "**Namespace/Module Path" ->
        { meta with Namespace = stripAndCleanDocLineForMeta "**Namespace/Module Path" l |> Some }

    | l when l.StartsWith "**Assembly" ->
        { meta with Assembly = stripAndCleanDocLineForMeta "**Assembly" l |> Some }
    | _ -> meta

let docToMeta doc =
    Utils.split doc '\n'
    |> Seq.fold fillMeta emptyMeta

// [!code-fsharp[Main](snippets/fslangref2/snippet7101.fs)]

let inlineSnippets doc =
    Regex.Matches (doc, "\[!code-fsharp\[\w+\]\((snippets/[\w\/]+.fs)\)\]")
    |> Seq.cast<Match>
    |> Seq.groupBy (fun m -> m.Value)
    |> Seq.map (fun (v, gs) ->
        let group = Seq.head gs
        let snippetFilename = (Seq.last group.Groups).Value
        v, snippetFilename)
    |> Seq.fold
        (fun (d : string) (value, snippetFilename) ->
            let snippetData = OrigDocPath + "/" + snippetFilename |> File.ReadAllText
            d.Replace (value, "```fsharp\n" + snippetData + "\n```"))
        doc

let htmlTemplate = """
<!DOCTYPE html>
<html>
<head>
    <title>{TITLE}</title>
    <link rel="stylesheet" href="./style.css">
</head>
<body>
{BODY}
</body>
</html>
"""

let toStandAloneHtml meta body =
    htmlTemplate
    |> replace "{TITLE}" meta.Title
    |> replace "{BODY}" body

let processFile (conn : SQLiteConnection) path =
    let doc = File.ReadAllText path |> inlineSnippets
    let meta = docToMeta doc
    let filename = path.Split '/' |> Seq.rev |> Seq.head |> replace ".md" ".html"
    let html = Markdown.TransformHtml doc |> toStandAloneHtml meta
    File.WriteAllText (DocPath + "/" + filename, html)

    conn.Insert(
        { Id = 0
          Name = meta.Title
          Type = meta.Type.ToString()
          Path = filename },
        "OR IGNORE") |> ignore

[<EntryPoint>]
let main _ =
    Directory.CreateDirectory DocPath |> ignore

    use conn = new SQLiteConnection(ResourcePath + "/docSet.dsidx")
    conn.DropTable<Entry>() |> ignore
    conn.CreateTable<Entry>() |> ignore
    conn.CreateIndex("anchor", "searchIndex", [| "name"; "type"; "path" |], true) |> ignore

    Directory.GetFiles (OrigDocPath, "*.md")
    |> Seq.iter (processFile conn)

    0 // return an integer exit code