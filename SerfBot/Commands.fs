module SerfBot.Commands

open System
open SerfBot.OpenAiApi;
open SerfBot.Types
open SerfBot.Configuration

let commandDescriptions =
    "!ping - команда проверки связи\n" +
    "!context - команда изменения контекста\n" +
    "!vision - команда анализа присланной картинки\n" +
    "!help - команда вывода списка команд\n" +
    "!uptime - команда показа количества дней работы бота\n" +
    "погода - команда получения погоды\n" +
    "гпт - команда для вопроса в ChatGpt\n";

let commandHandler command =
     try
        match command with
           | Ping -> "pong"
           | Vision (userText, photo) ->
                   let base64img = TelegramApi.base64FromFileId (Array.last photo.Value).FileId
                   descriptionAnalyzedImage userText base64img
                   |> Async.RunSynchronously
           | Context userText ->
                   setupContext userText
                   |> ignore
                   "Контекст сменен"
           | Question userText ->
                   gptAnswer userText
                   |> Async.RunSynchronously
           | Uptime -> $"Bot active is {(DateTime.Now.Date - startTime.Date).Days} days" 
           | Weather city ->
                   let weather = WeatherApi.getWeatherAsync city
                                 |> Async.RunSynchronously
                   $"Погода в %s{city}: %s{weather}"
           | HelpCommands -> commandDescriptions
           | _ -> "Некорректная команда"
         with
            | ex -> sprintf "Ошибка: %s" ex.Message