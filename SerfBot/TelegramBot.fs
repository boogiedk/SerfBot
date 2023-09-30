module SerfBot.TelegramBot

open Funogram.Api
open Funogram.Telegram
open Funogram.Telegram.Bot
open System.Collections.Generic
open SerfBot.Log
open SerfBot.OpenAiApi;
open SerfBot.Types
open Telegram.Bot.Types;
    

type CommandHandler = string -> string

let commandHandlers : Dictionary<string, CommandHandler> = Dictionary()

let addCommandHandler (command: string) (handler: CommandHandler) =
    commandHandlers.Add(command.ToLower(), handler)

let handlePingCommand (command: string) =
    match command.ToLower() with
    | "ping" -> "pong"
    | _ -> "Неизвестная команда"

let handleWeatherCommand (command: string) =
    match command.Split(" ", 2) with
    | [| "погода"; location |] ->
        try
            let weather = WeatherApi.getWeatherAsync location |> Async.RunSynchronously
            $"Погода в %s{location}: %s{weather}"
        with
        | ex -> sprintf "Ошибка при получении погоды: %s" ex.Message
        
    | _ -> "Неизвестная команда"

let handleGPTCommand (command: string) =
    match command.Split(" ", 2) with
    | [| "gpt"; inputText |] ->
        let replayText = gptAnswer inputText |> Async.RunSynchronously;
        $"%s{replayText}"
    | _ -> "Неизвестная команда"

let extractCommand (str: string) = str.Split(" ")[0];

addCommandHandler "ping" handlePingCommand
addCommandHandler "погода" handleWeatherCommand
addCommandHandler "gpt" handleGPTCommand

let processCommand (ctx: UpdateContext, command: MessageReplayCommand) =
   Api.sendMessageReply command.Chat.Id command.ReplayText command.MessageId 
    |> api ctx.Config
    |> Async.Ignore
    |> Async.Start  

let isValidUser (userId: int64) =
    if Array.contains userId Configuration.config.UserIds then Some ()
    else None

let updateArrived (ctx: UpdateContext) =
    match ctx.Update.Message with
    | Some { MessageId = messageId; Chat = chat; Text = text } ->
        let user = ctx.Update.Message.Value.From.Value
        match isValidUser user.Id with
        | Some () ->
            logInfo $"Message from user {Option.get user.Username} received: {Option.get text}"
            let lowerText = text.Value.ToLower()
            let words = extractCommand lowerText
            match commandHandlers.TryGetValue words with
            | true, handler ->
                let replyText = handler lowerText
                processCommand(ctx, { Chat = chat; MessageId = messageId; Text = text; ReplayText = replyText })
            | _ -> ()
        | None ->
            sprintf "Authorize error." |> logInfo
    | _ -> ()
    