﻿module SerfBot.Types

open System
open Funogram.Telegram
open Funogram.Telegram.Types

let mutable internal startTime = DateTime

type CityCoordinates =
    { CityName: string
      Latitude: float
      Longitude: float }

type CurrentWeather = {
    temperature: float
}

type MessageReplayCommand = {
  MessageId: int64
  Chat: Types.Chat
  Text: string option
  ReplayText: string
}

type GPTResponse = {
    generatedText: string
}

type WeatherData = { current_weather: CurrentWeather }

[<CLIMutable>]
type ApplicationConfiguration = {
    TelegramBotToken: string
    OpenAiApiToken: string
    UserIds: int64[]
    StarBotDatetime: DateTime 
}

type Command =
    | Ping
    | Question of string
    | Context of string
    | Vision of string * PhotoSize array option 
    | Weather of string
    | Uptime
    | HelpCommands
    | Other of string

