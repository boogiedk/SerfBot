module SerfBot.OpenAiApi

open OpenAI
open OpenAI.Managers
open OpenAI.ObjectModels
open OpenAI.ObjectModels.RequestModels
open SerfBot.Types
open OpenAI.Chat
open System
open Log


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

let conversationGPT =
    let token = Configuration.config.OpenAiApiToken
    let options = OpenAiOptions()
    options.ApiKey <- token
    
    let openApiClient = new OpenAIService(options)
    
    openApiClient
    
let gptQuestionRequest userText =
    let messages =
            [
                ChatMessage.FromSystem(Option.get currentContext)
                ChatMessage.FromUser(userText)
            ] |> List.toArray
    
    ChatCompletionCreateRequest(Messages = messages, Model = Models.Gpt_4)

let gptAnswer userQuestion =
    async {
        try
            let conv = conversationGPT
            let request = gptQuestionRequest userQuestion
            
            let! completionResult = conv.CreateCompletion(request) |> Async.AwaitTask
            
            let result =
                  if completionResult.Successful then
                      completionResult.Choices |> Seq.head |> fun c -> c.Message.Content
                  else
                      match completionResult.Error with
                      | null ->
                          "Unknown Error" |> logInfo
                          raise (Exception("Unknown Error"))
                      | error ->
                          $"{error.Code} {error.Message}" |> logInfo
                          sprintf "%s: %s" error.Code error.Message

            return result

        with
        | ex ->
            ex.Message |> logInfo
            return ex.Message
    }
 
let descriptionAnalyzedImage imageLink =
    async {
        try
            let api = OpenAIClient(Configuration.config.OpenAiApiToken)

            let messages =
                [
                    Message(Role.System, Option.get currentContext)
                    Message(Role.User,
                            [
                                Content(ContentType.Text, "Что на этой картинке?")
                                Content(ContentType.ImageUrl, imageLink)
                            ])
                ]
            
            let result =
                async { 
                    let! completionResult = api.ChatEndpoint.GetCompletionAsync(ChatRequest(messages, model = "gpt-4-vision-preview", maxTokens = 500)) |> Async.AwaitTask
                    return completionResult
                }
                |> Async.RunSynchronously
                
            let answer = result.FirstChoice.Message.Content.ToString()
            return answer
            
        with
        | ex ->
            ex.Message.ToString() |> logInfo
            return ex.Message.ToString()
    }    