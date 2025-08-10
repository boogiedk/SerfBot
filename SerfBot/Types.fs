module SerfBot.Types

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

[<CLIMutable>]
type ApplicationConfiguration = {
    TelegramBotToken: string
    OpenAiApiToken: string
    UserIds: int64[]
    StarBotDatetime: DateTime 
    LogChannelId: string
    Gpt2ApiUrl: string
    Gpt2ApiToken: string
}

type Command =
    | Ping
    | Question of string
    | Gpt2Question of string
    | Context of string
    | Vision of string * PhotoSize array option 
    | Weather of string
    | Uptime
    | HelpCommands
    | ClearConversationHistory
    | ResetHistory
    | GetModels
    | SetModel of string
    | Other of string

type ChatMessage = {
    Role: string
    Content: string
    Timestamp: DateTime
}

type ConversationHistory = {
    Messages: ChatMessage list
    MaxHistorySize: int
}

