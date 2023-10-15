module SerfBot.TelegramBot

open System
open ExtCore.Control.Collections
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

let isValidUser (userId: int64) =
    if Array.contains userId Configuration.config.UserIds then Some ()
    else None

let processCommand (ctx: UpdateContext, command: MessageReplayCommand) =
        sendReplayMessageFormatted command.ReplayText ParseMode.Markdown ctx.Config api command.Chat.Id command.MessageId
        |> Async.RunSynchronously
        |> ignore

let updateArrived (ctx: UpdateContext) =
    match ctx.Update.Message with
    | Some { MessageId = messageId; Chat = chat; Text = text } ->
        let user = ctx.Update.Message.Value.From.Value
        match isValidUser user.Id with
        | Some () ->
            logInfo $"Message from user {Option.get user.Username} received: {Option.get text}"
            let command, userMessage = extractCommand text.Value
            match Commands.commandHandlers.TryGetValue command with
            | true, handler ->
                let replyText = handler userMessage
                processCommand(ctx, { Chat = chat; MessageId = messageId; Text = text; ReplayText = replyText; })
            | _ -> ()
        | None ->
            sprintf "Authorize error." |> logInfo
    | _ -> ()
    