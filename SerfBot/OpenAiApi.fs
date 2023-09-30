module SerfBot.OpenAiApi

open OpenAI_API
open OpenAI_API.Models

let conversationGPT userText =
    let openApiClient = OpenAIAPI(Configuration.config.OpenAiApiToken)
    let conversation = openApiClient.Chat.CreateConversation();
    conversation.AppendUserInput(userText);
    conversation.RequestParameters.Temperature <- 0.9;
    conversation.RequestParameters.MaxTokens <- 1024;
    conversation.Model <- Model.ChatGPTTurbo;
    conversation;

let gptAnswer userQuestion =
    async {
    let conv = conversationGPT userQuestion
    let! result = conv.GetResponseFromChatbotAsync() |> Async.AwaitTask
    return result
   }
    