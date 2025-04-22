module SerfBot.TelegramApi

open Funogram.Telegram
open Funogram.Telegram.Types
open System
open System.IO
open System.Net.Http
open Funogram.Api
open Funogram.Telegram.Bot
open SerfBot.Types

let public sendMessageFormatted text parseMode config bot chatId =
  Req.SendMessage.Make(ChatId.Int chatId, text, parseMode = parseMode) |> bot config

let streamToBase64 (stream: Stream) =
    use ms = new MemoryStream()
    stream.CopyTo(ms)
    let buffer = ms.ToArray()
    Convert.ToBase64String(buffer)

let extractBase64File fileResult =
    match fileResult with
    | Ok file ->
        let filePath = Option.get file.FilePath
        let apiUrl = $"https://api.telegram.org/file/bot{Configuration.config.TelegramBotToken}/{filePath}"
        use httpStream = new HttpClient()
        httpStream.GetStreamAsync(apiUrl)
                |> Async.AwaitTask
                |> Async.RunSynchronously
                |> streamToBase64             
    | _ -> failwith "Error when getting file"
    
let base64FromFileId fileId =
                    Req.GetFile.Make fileId
                    |> api {Config.defaultConfig with Token = Configuration.config.TelegramBotToken }
                    |> Async.RunSynchronously
                    |> extractBase64File