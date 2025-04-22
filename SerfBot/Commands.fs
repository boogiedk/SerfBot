module SerfBot.Commands

open System
open SerfBot.OpenAiApi;
open SerfBot.Types
open SerfBot.Configuration

let commandDescriptions =
    "`!ping` - команда проверки связи\n" +
    "`!context` - команда изменения контекста\n" +
    "`!vision` - команда анализа присланной картинки\n" +
    "`!help` - команда вывода списка команд\n" +
    "`!uptime` - команда показа количества дней работы бота\n" +
    "`гпт` - команда для вопроса в ChatGpt\n";

let commandHandler command =
    match command with
    | Ping -> Ok "pong"
    | Vision (userText, Some photos) when photos.Length > 0 ->
        let base64img = TelegramApi.base64FromFileId (Array.last photos).FileId
        descriptionAnalyzedImage userText base64img
        |> Async.RunSynchronously
        |> Ok
    | Vision (_, None) | Vision (_, Some [||]) -> Error "Необходимо отправить изображение для анализа."
    | Context userText -> setupContext userText |> ignore; Ok "Контекст сменен"
    | Question userText -> gptAnswer userText |> Async.RunSynchronously |> Ok
    | Uptime -> Ok $"Bot active is {(DateTime.Now.Date - startTime.Date).Days} days"
    | HelpCommands -> Ok commandDescriptions
    | _ -> Error "Некорректная команда"
    
    