namespace SerfBot

open Funogram.Api
open Funogram.Telegram
open Funogram.Telegram.Bot
open Funogram.Types
open SerfBot.Log
open SerfBot.TelegramBot
open Types
open Serilog

module Program = 

    [<EntryPoint>]
    let main _ =
      async {
        logger <- LoggerConfiguration()
             .MinimumLevel.Information()
             .WriteTo.Console()
             .CreateLogger()
             
        let telegramBotConfig = {Config.defaultConfig with Token = Configuration.config.TelegramBotToken }
        let! _ = Api.deleteWebhookBase () |> api telegramBotConfig
      
        logInfo "SerfBot start"
        return! startBot telegramBotConfig updateArrived None
      } |> Async.RunSynchronously
      
      logInfo "SefBot stopped"
      0
