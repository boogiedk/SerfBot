module SerfBot.TelegramApi

open Funogram.Telegram
open Funogram.Telegram.Types

let public sendMessageFormatted text parseMode config bot chatId =
  Req.SendMessage.Make(ChatId.Int chatId, text, parseMode = parseMode) |> bot config

let public sendReplayMessageFormatted text parseMode config bot chatId replyToMessageId =
  Req.SendMessage.Make(ChatId.Int chatId, text, replyToMessageId = replyToMessageId, parseMode = parseMode) |> bot config

