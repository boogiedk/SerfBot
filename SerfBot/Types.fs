module SerfBot.Types

open Funogram.Telegram

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
}

type CommandHandler = string -> string

type Commands = {
    Name: string
    Handler: CommandHandler
}

type Command =
    | Ping
    | Question of string
    | Context of string
    | Vision of string * string
    | Weather of string
    | Other of string

