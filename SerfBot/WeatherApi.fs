module SerfBot.WeatherApi

open System.Net.Http
open System.Text.Json
open System.Globalization
open SerfBot.Types;

let cities =
    [ { CityName = "слуцк"; Latitude = 53.02; Longitude = 27.55 }
      { CityName = "минск"; Latitude = 53.90; Longitude = 27.55 }
      { CityName = "москва"; Latitude = 55.75; Longitude = 37.61 }
      { CityName = "ростов-на-дону"; Latitude = 47.23; Longitude = 39.70 } ]

let getCoordinates cityName =
    cities
    |> List.tryFind (fun city -> city.CityName = cityName)
    
let extractCity (str: string) : string =
    str.Split(" ")[1]    

let getWeatherAsync (location: string) =
    async {
        let loc = getCoordinates(location)
        let url = $"https://api.open-meteo.com/v1/forecast?latitude={loc.Value.Latitude.ToString(CultureInfo.InvariantCulture)}&longitude={loc.Value.Longitude.ToString(CultureInfo.InvariantCulture)}&current_weather=true&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
        let httpClient = new HttpClient();
        let! response =
            httpClient.GetStreamAsync(url)
            |> Async.AwaitTask;

        let weatherData = JsonSerializer.Deserialize<WeatherData>(response)

        let weatherInfo = 
            match weatherData with
            | data -> sprintf "Температура: %.2f°C" data.current_weather.temperature

        return weatherInfo
    }