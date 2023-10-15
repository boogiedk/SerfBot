module SerfBot.OpenAiApi

open OpenAI_API
open OpenAI_API.Models
open SerfBot.Log

let defaultContext = "Ты персональный помощник-бот в telegram. Чаще всего тебе нужно генерировать C#, F# или SQL код, но иногда нужно и отвечать на бытовые вопросы."
let mutable currentContext = Some(defaultContext)

let setupContext (newContext: string) =
    match newContext with
    | null -> 
        match currentContext with
        | None -> defaultContext
        | Some x -> x
    | _ -> 
        currentContext <- Some newContext
        newContext

let conversationGPT userText =
    let openApiClient = OpenAIAPI(Configuration.config.OpenAiApiToken)
    let conversation = openApiClient.Chat.CreateConversation()
    conversation.AppendSystemMessage(Option.get currentContext)
    conversation.AppendUserInput(userText);
    conversation.RequestParameters.Temperature <- 0.9;
    conversation.RequestParameters.MaxTokens <- 1024;
    conversation.Model <- Model.GPT4;
    conversation;

let gptAnswer userQuestion =
    async {
        try
            let conv = conversationGPT userQuestion
            let! result = conv.GetResponseFromChatbotAsync() |> Async.AwaitTask
            return result
        with
            | ex ->
                let errorText = sprintf "Exception text: %s" (ex.Message)
                logErr errorText
                return errorText
    }  