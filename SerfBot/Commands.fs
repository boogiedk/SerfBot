module SerfBot.Commands

open ExtCore.Control.Collections
open SerfBot.OpenAiApi;
open SerfBot.Types

let commandHandler command =
     try
        match command with
           | Ping -> "pong"
           | Vision (userText, base64Img) ->
                   descriptionAnalyzedImage userText base64Img
                   |> Async.RunSynchronously
           | Context userText ->
                   setupContext userText
                   |> ignore
                   "Контекст сменен"
           | Question userText ->
                   gptAnswer userText
                   |> Async.RunSynchronously
           | Weather city ->
                   let weather = WeatherApi.getWeatherAsync city
                                 |> Async.RunSynchronously
                   $"Погода в %s{city}: %s{weather}"
           | _ -> "Некорректная команда"
         with
            | ex -> sprintf "Ошибка: %s" ex.Message