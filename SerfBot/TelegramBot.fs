module SerfBot.TelegramBot

open System
open System.Runtime.CompilerServices
open ExtCore.Control.Collections
open Funogram.Api
open Funogram.Telegram
open Funogram.Telegram.Bot
open System.Collections.Generic
open Funogram.Telegram.Types
open SerfBot.Log
open SerfBot.OpenAiApi;
open SerfBot.Types
open Telegram.Bot.Types;
open System.Text.RegularExpressions
open SerfBot.TelegramApi

type CommandHandler = string -> string

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

let commandHandlers =
    dict
        [
          "ping", handlePingCommand
          "погода", handleWeatherCommand
          "гпт", handleGPTCommand
        ]

let extractCommand (str: string) = (str.Split(" ")[0]).Trim().ToLower();

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
            let userMessage = text.Value;
            let command = extractCommand userMessage
            match commandHandlers.TryGetValue command with
            | true, handler ->
                let replyText = handler userMessage
                processCommand(ctx, { Chat = chat; MessageId = messageId; Text = text; ReplayText = replyText; })
            | _ -> ()
        | None ->
            sprintf "Authorize error." |> logInfo
    | _ -> ()
    