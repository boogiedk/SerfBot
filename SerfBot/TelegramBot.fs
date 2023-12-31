﻿module SerfBot.TelegramBot

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

let isValidUser (userId: int64) =
    if Array.contains userId Configuration.config.UserIds then Some ()
    else None

let processCommand (ctx: UpdateContext, command: MessageReplayCommand) =
        sendReplayMessageFormatted command.ReplayText ParseMode.Markdown ctx.Config api command.Chat.Id command.MessageId
        |> Async.RunSynchronously
        |> ignore          
            
let updateArrivedMessage (ctx: UpdateContext) =
     match ctx.Update.Message with
        | Some { MessageId = messageId; Chat = chat; Text = text; Photo = photo; Caption = caption; From = from } ->
            let user = from.Value;
            let message = if text.IsSome then text.Value elif caption.IsSome then caption.Value else ""
            match isValidUser user.Id with
            | Some () ->
                logInfo $"Message from user {Option.get user.Username} received: {message}"
                let command, userMessage = extractCommand message
                let commandType =
                    match command with
                    | "!ping" -> Ping
                    | "!context" -> Context userMessage
                    | "!vision" -> Vision (userMessage, photo)
                    | "!help" -> HelpCommands
                    | "!uptime" -> Uptime
                    | "погода" -> Weather userMessage
                    | "гпт" -> Question userMessage
                    | _ -> Other userMessage
                    
                let replyText = Commands.commandHandler commandType
                processCommand(ctx, { Chat = chat; MessageId = messageId; Text = text; ReplayText = replyText })
            | None -> sprintf "Authorize error." |> logInfo
            | _ -> ()
        | None -> sprintf "Error." |> logInfo
        | _ -> ()