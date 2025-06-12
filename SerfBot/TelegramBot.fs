module SerfBot.TelegramBot

open Funogram.Api
open Funogram.Telegram.Bot
open Funogram.Telegram.Types
open SerfBot.Log
open SerfBot.Types
open SerfBot.TelegramApi

let extractCommand (str: string) =
    if str.StartsWith("!") || str.StartsWith("гпт") then
        match str.Split(" ", 2) with
        | [| command; inputText |] -> (command, inputText)
        | [| command; |] -> (command, null)
        | _ -> failwith "todo"
    else
        ("default", str)

let isValidUser (userId: int64) =
    if Array.contains userId Configuration.config.UserIds then Some ()
    else None

let processCommand (ctx: UpdateContext, command: MessageReplayCommand) =
        sendMessageFormatted command.ReplayText ParseMode.Markdown ctx.Config api command.Chat.Id
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
            | "!clear" -> ClearConversationHistory
            | "гпт" -> Question userMessage
            | "гпт2" -> Gpt2Question userMessage
            | "default" -> Gpt2Question userMessage
            | _ -> Other userMessage

        let replyText =
            match Commands.commandHandler commandType with
            | Ok text -> text
            | Microsoft.FSharp.Core.Error err -> $"Ошибка: {err}"

        processCommand(ctx, { Chat = chat; MessageId = messageId; Text = Some message; ReplayText = replyText })
    | None -> logInfo $"Authorize error. User: {user.Id} - {user.Username} Message: {message}"
 
            
let updateArrivedMessage (ctx: UpdateContext) =
    match ctx.Update with
    | { Message = Some { MessageId = messageId; Chat = chat; From = Some user; Text = Some text } } when chat.Type = ChatType.Private ->
        processIncomingMessage(ctx, messageId, chat, user, text, None)
    | { Message = Some { MessageId = messageId; Chat = chat; From = Some user; Caption = Some caption; Photo = photo } } when chat.Type = ChatType.Private ->
        processIncomingMessage(ctx, messageId, chat, user, caption, photo)
    | { Message = Some { From = None } } -> logInfo "Сообщение от неизвестного отправителя."
    | { Message = None } -> ();
    | _ -> ()
