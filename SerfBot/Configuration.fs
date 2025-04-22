namespace SerfBot

open Funogram.Telegram
open Microsoft.Extensions.Configuration
open Types
open System
open SerfBot.Log
open System.IO

module Configuration =
    let mutable configBuilder =
            ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true)
    
    let config = configBuilder
                        .Build()
                        .Get<ApplicationConfiguration>()
                
    let mutable startTime = DateTime.Now            
    