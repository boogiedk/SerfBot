module SerfBot.Gpt2Api

open System
open System.Net.Http
open System.Text
open System.Text.Json
open System.Text.Json.Serialization
open SerfBot.Configuration
open SerfBot.Log

[<JsonConverter(typeof<JsonStringEnumConverter>)>]
type ClientType = 
    | SerfBot = 1
    | Other = 0

[<CLIMutable>]
type ChatRequest = {
    [<JsonPropertyName("message")>]
    Message: string
}

[<CLIMutable>]
type ApiRequest = {
    [<JsonPropertyName("request")>]
    Request: ChatRequest
}

[<CLIMutable>]
type McpResponse = {
    [<JsonPropertyName("errorMessage")>]
    ErrorMessage: string
    [<JsonPropertyName("content")>]
    Content: string
} with
    member this.IsError = not (String.IsNullOrEmpty this.ErrorMessage)

[<CLIMutable>]
type ResetResponse = {
    [<JsonPropertyName("success")>]
    Success: bool
    [<JsonPropertyName("message")>]
    Message: string
}

[<CLIMutable>]
type ModelInfo = {
    [<JsonPropertyName("id")>]
    Id: string
    [<JsonPropertyName("object")>]
    Object: string
    [<JsonPropertyName("owned_by")>]
    OwnedBy: string
}

[<CLIMutable>]
type ModelsResponse = {
    [<JsonPropertyName("success")>]
    Success: bool
    [<JsonPropertyName("models")>]
    Models: string array
    [<JsonPropertyName("message")>]
    Message: string
}

[<CLIMutable>]
type SetModelRequest = {
    [<JsonPropertyName("modelName")>]
    ModelName: string
}

[<CLIMutable>]
type SetModelResponse = {
    [<JsonPropertyName("success")>]
    Success: bool
    [<JsonPropertyName("message")>]
    Message: string
    [<JsonPropertyName("previousModel")>]
    PreviousModel: string
    [<JsonPropertyName("currentModel")>]
    CurrentModel: string
}

let private httpClient = new HttpClient()

let private jsonOptions = 
    let options = JsonSerializerOptions()
    options.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
    options.Converters.Add(JsonStringEnumConverter())
    options

let private addAuthorizationHeader (client: HttpClient) =
    client.DefaultRequestHeaders.Authorization <- 
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.Gpt2ApiToken)

let processMessage (message: string) : Async<Result<string, string>> =
    async {
        try
            addAuthorizationHeader httpClient
            
            let request = {
                Message = message
            }
            
            let json = JsonSerializer.Serialize(request, jsonOptions)
            let content = new StringContent(json, Encoding.UTF8, "application/json")
            let url = $"{config.Gpt2ApiUrl}/chats/process"
            
            logInfo $"Sending request to GPT2 API: {message}"
            
            let! response = httpClient.PostAsync(url, content) |> Async.AwaitTask
            let! responseContent = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            
            if response.IsSuccessStatusCode then
                let mcpResponse = JsonSerializer.Deserialize<McpResponse>(responseContent, jsonOptions)
                
                if mcpResponse.IsError then
                    logError $"GPT2 API error: {mcpResponse.ErrorMessage}"
                    return Error mcpResponse.ErrorMessage
                else
                    logInfo "GPT2 API request completed successfully"
                    return Ok mcpResponse.Content
            else
                let errorMsg = $"HTTP Error {response.StatusCode}: {responseContent}"
                logError errorMsg
                return Error errorMsg
                
        with
        | ex ->
            let errorMsg = $"Exception during GPT2 API call: {ex.Message}"
            logError errorMsg
            return Error errorMsg
    }

let gpt2Answer (userText: string) : Async<string> =
    async {
        if String.IsNullOrWhiteSpace(userText) then
            return "Пожалуйста, укажите сообщение для обработки."
        else
            let! result = processMessage userText
            match result with
            | Ok content -> return content
            | Error errorMsg -> return $"Ошибка при обращении к GPT2: {errorMsg}"
    }

let resetHistory () : Async<Result<string, string>> =
    async {
        try
            addAuthorizationHeader httpClient
            
            let url = $"{config.Gpt2ApiUrl}/chats/clear-history"
            logInfo "Sending request to reset chat history"
            
            let! response = httpClient.PostAsync(url, null) |> Async.AwaitTask
            let! responseContent = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            
            if response.IsSuccessStatusCode then
                let resetResponse = JsonSerializer.Deserialize<ResetResponse>(responseContent, jsonOptions)
                
                if resetResponse.Success then
                    logInfo "Chat history reset successfully"
                    return Ok resetResponse.Message
                else
                    let errorMsg = "Failed to reset chat history"
                    logError errorMsg
                    return Error errorMsg
            else
                let errorMsg = $"HTTP Error {response.StatusCode}: {responseContent}"
                logError errorMsg
                return Error errorMsg
                
        with
        | ex ->
            let errorMsg = $"Exception during reset history call: {ex.Message}"
            logError errorMsg
            return Error errorMsg
    }

let getModels () : Async<Result<string, string>> =
    async {
        try
            addAuthorizationHeader httpClient
            
            let url = $"{config.Gpt2ApiUrl}/openai/models"
            logInfo "Sending request to get available models"
            
            let! response = httpClient.GetAsync(url) |> Async.AwaitTask
            let! responseContent = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            
            if response.IsSuccessStatusCode then
                let modelsResponse = JsonSerializer.Deserialize<ModelsResponse>(responseContent, jsonOptions)
                
                if modelsResponse.Success then
                    logInfo $"Retrieved {modelsResponse.Models.Length} models successfully"
                    let modelsList = String.Join("\n", modelsResponse.Models)
                    return Ok $"Доступные модели ({modelsResponse.Models.Length}):\n\n{modelsList}"
                else
                    let errorMsg = "Failed to retrieve models"
                    logError errorMsg
                    return Error errorMsg
            else
                let errorMsg = $"HTTP Error {response.StatusCode}: {responseContent}"
                logError errorMsg
                return Error errorMsg
                
        with
        | ex ->
            let errorMsg = $"Exception during get models call: {ex.Message}"
            logError errorMsg
            return Error errorMsg
    }

let setModel (modelName: string) : Async<Result<string, string>> =
    async {
        try
            if String.IsNullOrWhiteSpace(modelName) then
                return Error "Необходимо указать название модели"
            else
                addAuthorizationHeader httpClient
                
                let request = { ModelName = modelName }
                let json = JsonSerializer.Serialize(request, jsonOptions)
                let content = new StringContent(json, Encoding.UTF8, "application/json")
                
                let url = $"{config.Gpt2ApiUrl}/openai/set-model"
                logInfo $"Sending request to set model to: {modelName}"
                
                let! response = httpClient.PostAsync(url, content) |> Async.AwaitTask
                let! responseContent = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                
                if response.IsSuccessStatusCode then
                    let setModelResponse = JsonSerializer.Deserialize<SetModelResponse>(responseContent, jsonOptions)
                    
                    if setModelResponse.Success then
                        logInfo $"Model changed successfully from '{setModelResponse.PreviousModel}' to '{setModelResponse.CurrentModel}'"
                        return Ok setModelResponse.Message
                    else
                        let errorMsg = "Failed to set model"
                        logError errorMsg
                        return Error errorMsg
                else
                    let errorMsg = $"HTTP Error {response.StatusCode}: {responseContent}"
                    logError errorMsg
                    return Error errorMsg
                    
        with
        | ex ->
            let errorMsg = $"Exception during set model call: {ex.Message}"
            logError errorMsg
            return Error errorMsg
    }
