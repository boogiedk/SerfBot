module SerfBot.TelegramBot

open System
open System.IO
open System.Net.Http
open ExtCore.Control.Collections
open Funogram.Api
open Funogram.Telegram
open Funogram.Telegram.Bot
open Funogram.Telegram.Types
open SerfBot.Log
open SerfBot.Types
open SerfBot.TelegramApi

let extractCommand (str: string) =
     match str.Split(" ", 2) with
     | [| command; inputText |] ->
         (command, inputText)
     | [| command; |] ->
         (command, null)

let isValidUser (userId: int64) =
    if Array.contains userId Configuration.config.UserIds then Some ()
    else None

let processCommand (ctx: UpdateContext, command: MessageReplayCommand) =
        sendReplayMessageFormatted command.ReplayText ParseMode.Markdown ctx.Config api command.Chat.Id command.MessageId
        |> Async.RunSynchronously
        |> ignore

let streamToBase64 (stream: Stream) =
    use ms = new MemoryStream()
    stream.CopyTo(ms)
    let buffer = ms.ToArray()
    Convert.ToBase64String(buffer)

let extractFileDataAsBase64 (fileResult: Result<File,Funogram.Types.ApiResponseError>) =
    match fileResult with
    | Ok(file) ->
        let filePath = Option.get file.FilePath // предполагается, что у вас есть свойство "Path" в типе "File"
        let apiUrl = $"https://api.telegram.org/file/bot{Configuration.config.TelegramBotToken}/{filePath}"
        use httpStream = new HttpClient()
        let f = httpStream.GetStreamAsync(apiUrl) |> Async.AwaitTask  |> Async.RunSynchronously
        let base64 = streamToBase64 f
        base64

    
    
let handleFiles fileId ctx =
     let file = Req.GetFile.Make fileId
                    |> api ctx.Config
                    |> Async.RunSynchronously
     let base64Img = extractFileDataAsBase64 file
     base64Img
            
            
let updateArrivedMessage (ctx: UpdateContext) =
     match ctx.Update.Message with
        | Some { MessageId = messageId; Chat = chat; Text = text; Photo = photo; Caption = caption } ->
            let user = ctx.Update.Message.Value.From.Value
            
            let base64Img = if photo.IsSome then handleFiles (Array.last photo.Value).FileId ctx else ""
            let message = if text.IsSome then text.Value elif caption.IsSome then caption.Value else ""
            match isValidUser user.Id with
            | Some () ->
                logInfo $"Message from user {Option.get user.Username} received: {message}"
                let command, userMessage = extractCommand message
                let commandType =
                    match command with
                    | "!ping" -> Ping
                    | "погода" -> Weather userMessage
                    | "!context" -> Context userMessage
                    | "!vision" -> Vision (userMessage, base64Img)
                    | "гпт" -> Question userMessage
                    | _ -> Other userMessage
                    
                let replyText = Commands.commandHandler commandType
                processCommand(ctx, { Chat = chat; MessageId = messageId; Text = text; ReplayText = replyText })
            | _ -> ()
        | None -> sprintf "Authorize error." |> logInfo
        | _ -> ()