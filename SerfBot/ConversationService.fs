module SerfBot.ConversationService

open OpenAI
open OpenAI.Chat
open OpenAI.ObjectModels.RequestModels
open SerfBot.Types
open System
open Log

type ConversationService() =
    let mutable conversationHistory = {
        Messages = []
        MaxHistorySize = 15
    }

    member this.AddMessageToHistory role content =
        let newMessage = {
            Role = role
            Content = content
            Timestamp = DateTime.Now
        }
        let updatedMessages = newMessage :: conversationHistory.Messages
        let trimmedMessages = 
            if List.length updatedMessages > conversationHistory.MaxHistorySize then
                updatedMessages |> List.take conversationHistory.MaxHistorySize
            else
                updatedMessages
        conversationHistory <- { conversationHistory with Messages = trimmedMessages }

    member this.ClearHistory() =
        conversationHistory <- { conversationHistory with Messages = [] }

    member this.GetHistoryMessages() =
        conversationHistory.Messages
        |> List.map (fun msg -> 
            match msg.Role with
            | "user" -> ChatMessage.FromUser(msg.Content, null)
            | "assistant" -> ChatMessage.FromAssistant(msg.Content)
            | _ -> ChatMessage.FromSystem(msg.Content))
        |> List.toArray

    member this.GetHistorySize() = List.length conversationHistory.Messages

let conversationService = ConversationService() 