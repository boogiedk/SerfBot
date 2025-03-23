module SerfBot.TelegramBot

open System
open Funogram.Api
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
     | _ -> failwith "todo"

let isValidUser (userId: int64) =
    if Array.contains userId Configuration.config.UserIds then Some ()
    else None

let processCommand (ctx: UpdateContext, command: MessageReplayCommand) =
        sendReplayMessageFormatted command.ReplayText ParseMode.Markdown ctx.Config api command.Chat.Id command.MessageId
        |> Async.RunSynchronously
        |> ignore          

let processIncomingMessage(ctx, messageId, chat, user: User, message, photo) =
    match isValidUser user.Id with
    | Some () ->
        logInfo $"Message from user {Option.get user.Username} received: {message} "
        let command, userMessage = extractCommand message
        let commandType =
            match command with
            | "!ping" -> Ping
            | "!context" -> Context userMessage
            | "!vision" -> Vision (userMessage, photo)
            | "!help" -> HelpCommands
            | "!uptime" -> Uptime
            | "гпт" -> Question userMessage
            | _ -> Other userMessage

        let replyText =
            match Commands.commandHandler commandType with
            | Ok text -> text
            | Microsoft.FSharp.Core.Error err -> $"Ошибка: {err}"

        processCommand(ctx, { Chat = chat; MessageId = messageId; Text = Some message; ReplayText = replyText })
    | None -> logInfo "Authorize error."
 
 
            
let updateArrivedMessage (ctx: UpdateContext) =
    match ctx.Update.Message with
    | Some { MessageId = messageId; Chat = chat; From = Some user; Text = Some text } ->
        processIncomingMessage(ctx, messageId, chat, user, text, None)
    | Some { MessageId = messageId; Chat = chat; From = Some user; Caption = Some caption; Photo = photo } ->
        processIncomingMessage(ctx, messageId, chat, user, caption, photo)
    | Some { From = None } -> logInfo "Сообщение от неизвестного отправителя."
    | None -> logInfo "Ошибка: сообщение не найдено."
    | Some _ -> logInfo "todo"
