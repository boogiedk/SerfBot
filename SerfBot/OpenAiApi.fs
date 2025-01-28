module SerfBot.OpenAiApi

open OpenAI
open OpenAI.Chat
open OpenAI.Managers
open OpenAI.ObjectModels
open OpenAI.ObjectModels.RequestModels
open SerfBot.Types
open System
open Log

let defaultContext = "Ты персональный помощник-бот в telegram."
let mutable currentContext = Some(defaultContext)

let setupContext (newContext: string) =
    match newContext with
    | null | ""  -> 
        currentContext <- Some defaultContext
    | _ -> 
        currentContext <- Some newContext

let conversationGPT =
    let token = Configuration.config.OpenAiApiToken
    let options = OpenAiOptions()
    options.ApiKey <- token
    
    let openApiClient = new OpenAIService(options)
    
    openApiClient
    
let gptQuestionRequest userText =
    let messages =
            [
                ChatMessage.FromSystem(currentContext |> Option.defaultValue defaultContext)
                ChatMessage.FromUser(userText, null)
            ] |> List.toArray
    
    ChatCompletionCreateRequest(Messages = messages, Model = Models.Gpt_4o_mini)

let gptAnswer userQuestion =
    async {
        try
            let conv = conversationGPT
            let request = gptQuestionRequest userQuestion
            
            let! completionResult = conv.CreateCompletion(request) |> Async.AwaitTask
            
            let result =
                  if completionResult.Successful then
                      completionResult.Choices
                      |> Seq.head
                      |> fun c -> c.Message.Content
                  else
                      match completionResult.Error with
                      | null ->
                          let errorMessage = "Unknown Error"
                          errorMessage |> logInfo
                          errorMessage
                      | error ->
                          let errorMessage = $"{error.Code} {error.Message}"
                          errorMessage |> logInfo
                          errorMessage

            return result
        with
        | ex ->
            ex.Message |> logInfo
            return ex.Message
    }
 
let descriptionAnalyzedImage userText base64Img =
    async {
        try
            let api = new OpenAIClient(Configuration.config.OpenAiApiToken)
            let userText2 = if String.IsNullOrEmpty(userText) then "Что на фото?" else userText
            let messages =
                [
                    Message(Role.System, currentContext |> Option.defaultValue defaultContext)
                    Message(Role.User,
                            [
                                Content(ContentType.Text, userText2)
                                Content(ContentType.ImageUrl, $"data:image/jpeg;base64,{base64Img}")
                            ])
                ]
            
            let result =
                async { 
                    let! completionResult = api.ChatEndpoint.GetCompletionAsync(ChatRequest(messages, model = "gpt-4o-mini", maxTokens = 500)) |> Async.AwaitTask
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