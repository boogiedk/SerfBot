namespace SerfBot

open Funogram.Telegram
open Microsoft.Extensions.Configuration
open Types;

module Configuration =
    let mutable configBuilder =
            ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true)
    
    let config =
            configBuilder
                .Build()
                .Get<ApplicationConfiguration>();
    