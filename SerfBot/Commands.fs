module SerfBot.Commands

open ExtCore.Control.Collections
open SerfBot.OpenAiApi;

let handlePingCommand (command: string) =
    match command.ToLower() with
    | "ping" -> "pong"
    | _ -> "Неизвестная команда"

let handleWeatherCommand (command: string) =
    match command.Split(" ", 2) with
    | [| "погода"; location |] ->
        try
            let weather = WeatherApi.getWeatherAsync location
                          |> Async.RunSynchronously
            $"Погода в %s{location}: %s{weather}"
        with
        | ex -> sprintf "Ошибка при получении погоды: %s" ex.Message
        
    | _ -> "Неизвестная команда"

let handleGPTCommand (command: string) =
    match command.Split(" ", 2) with
    | [| "гпт"; inputText |] ->
        gptAnswer inputText
        |> Async.RunSynchronously;
    | _ -> "Неизвестная команда"
     
let handleContextCommand(command: string) =
      match command.Split(" ", 2) with
      | [| "!context"; inputText |] ->
        setupContext inputText |> ignore
        "Контекст сменен"
      | _ -> "Неизвестная команда"
      
let commandHandlers =
    dict
        [
          "ping", handlePingCommand
          "погода", handleWeatherCommand
          "гпт", handleGPTCommand
          "!context", handleContextCommand
        ]

