module O21.Localization.Translations

open FSharp.Data
open System.IO

type TranslationLanguageType = 
    | Json

type HelpRequestType =
    | MarkdownHelp
    | RussianHelp
                    
type Language = { 
    Name: string
    Type: TranslationLanguageType
    HelpRequestType: HelpRequestType
}

type private Provider = JsonProvider<"Localization/Translations/english.json">

let private rootFolder = "Localization/Translations/"

let Translation(language: Language) =
    Provider.Load (match language.Type with
                    | Json -> $"{rootFolder}{language.Name.ToLowerInvariant()}.json")

let AvailableLanguages = 
    let files = Directory.GetFiles(rootFolder, "*.json")
    seq {
        for file in files do
            let fileName = (Path.GetFileNameWithoutExtension file).ToLowerInvariant()
            yield { 
                Name = fileName
                Type = TranslationLanguageType.Json
                HelpRequestType = match fileName with
                                | "russian" -> HelpRequestType.RussianHelp
                                | _ -> HelpRequestType.MarkdownHelp
            }
    } |> Seq.cache

let DefaultLanguage = 
    System.Linq.Enumerable.First(AvailableLanguages, fun language -> language.Name = "english")
