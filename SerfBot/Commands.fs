module SerfBot.Commands

open ExtCore.Control.Collections
open SerfBot.OpenAiApi;


let pingCommand() =
    "pong"

let handlePingCommand (command: string) = "pong"

let handleWeatherCommand (userText: string) =
        try
            let weather = WeatherApi.getWeatherAsync userText
                          |> Async.RunSynchronously
            $"Погода в %s{userText}: %s{weather}"
        with
        | ex -> sprintf "Ошибка: %s" ex.Message

let handleGPTCommand (userText: string) =
        try
            gptAnswer userText
            |> Async.RunSynchronously
        with
        | ex -> sprintf "Ошибка: %s" ex.Message
     
let handleContextCommand(userText: string) =
        try
            setupContext userText
            |> ignore
            "Контекст сменен"
        with
        | ex -> sprintf "Ошибка: %s" ex.Message
      
let commandHandlers =
    dict
        [
          "ping", handlePingCommand
          "погода", handleWeatherCommand
          "гпт", handleGPTCommand
          "!context", handleContextCommand
        ]

