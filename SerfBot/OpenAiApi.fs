module SerfBot.OpenAiApi

open OpenAI
open OpenAI.Chat
open OpenAI.Managers
open OpenAI.ObjectModels.RequestModels
open SerfBot.Types
open SerfBot.ConversationService
open System
open Log

let defaultContext = "
    Ты — мой личный помощник и опытный разработчик уровня Senior/Architect. Твоя специализация — C#, .NET, DevOps, архитектура ПО, паттерны проектирования, рефакторинг, оценка и генерация кода.
    Помимо вопросов, ты будешь получать историю диалога, чтобы быть в контексте разговора - учти это при ответах.
    
    При ответах будь:
    — Лаконичным, но информативным.
    — Аргументированным: всегда объясняй почему ты рекомендуешь то или иное.
    — Используй плюсы/минусы, если рассматриваются альтернативы.
    — Не бойся делать архитектурные замечания, предлагать улучшения.
    — Примеры кода — минимально необходимые, но качественные.
    — Отвечай так, будто ты ментор, а не справка из MSDN.
    
    Ты можешь:
    — Давать рекомендации по архитектуре.
    — Объяснять паттерны.
    — Анализировать и улучшать код.
    — Помогать с DevOps вопросами.
    — Выбирать технологии под задачу.
    — Сравнивать подходы и фреймворки.
    
    Старайся отвечать в одном сообщении, но можешь уточнить.
    Если можно предложить best practice — предложи.
    Если подход спорный — укажи риски."

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

let gptAnswer userQuestion =
    async {
        try
            let conv = conversationGPT
            let historyMessages = conversationService.GetHistoryMessages()
            let messages =
                [|
                    ChatMessage.FromSystem(currentContext |> Option.defaultValue defaultContext)
                    yield! historyMessages
                    ChatMessage.FromUser(userQuestion, null)
                |]
            let request = ChatCompletionCreateRequest(Messages = messages, Model = "gpt-4.1")
            
            let! completionResult = conv.CreateCompletion(request) |> Async.AwaitTask
            
            let result =
                  if completionResult.Successful then
                      let answer = completionResult.Choices
                                  |> Seq.head
                                  |> fun c -> c.Message.Content
                      conversationService.AddMessageToHistory "user" userQuestion
                      conversationService.AddMessageToHistory "assistant" answer
                      answer
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
                    let! completionResult = api.ChatEndpoint.GetCompletionAsync(ChatRequest(messages, model = "gpt-4.1", maxTokens = 500)) |> Async.AwaitTask
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