module SerfBot.TelegramSink

open Serilog.Core
open Serilog.Events
open Funogram.Telegram
open Funogram.Telegram.Bot
open Funogram.Telegram.Types
open Funogram.Api
open SerfBot.Configuration

type TelegramSink() =
    interface ILogEventSink with
        member this.Emit(logEvent: LogEvent) =
                let message = logEvent.RenderMessage()
                let level = logEvent.Level.ToString()
                let timestamp = logEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
                let formattedMessage = $"SerfBot: *[{level}]* {timestamp}\n{message}"
                
                let config = { Config.defaultConfig with Token = config.TelegramBotToken }
                async {
                    try
                        let! result = Req.SendMessage.Make(ChatId.String Configuration.config.LogChannelId, formattedMessage, parseMode = ParseMode.Markdown)
                                    |> api config
                        match result with
                        | Ok _ -> ()
                        | Error e -> printfn $"Error sending log message: %A{e}"
                    with
                    | ex -> printfn $"Exception sending log message: %A{ex}"
                } |> Async.Start