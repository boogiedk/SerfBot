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

let private httpClient = new HttpClient()

let private jsonOptions = 
    let options = JsonSerializerOptions()
    options.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
    options.Converters.Add(JsonStringEnumConverter())
    options

let processMessage (message: string) : Async<Result<string, string>> =
    async {
        try
            let request = {
                Message = message
            }
            
            let json = JsonSerializer.Serialize(request, jsonOptions)
            let content = new StringContent(json, Encoding.UTF8, "application/json")
            
            logInfo $"Sending request to GPT2 API: {message}"
            
            let! response = httpClient.PostAsync(config.Gpt2ApiUrl, content) |> Async.AwaitTask
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
