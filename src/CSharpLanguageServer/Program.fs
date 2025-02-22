module CSharpLanguageServer.Program

open System.Globalization
open System.Threading
open Argu
open System.Reflection
open Serilog
open State

[<EntryPoint>]
let entry args =
    try
        Log.Logger <- LoggerConfiguration()
            .WriteTo.File("csharp-ls.log")
            .CreateLogger()
        
        let culture = CultureInfo("en-US")
        Thread.CurrentThread.CurrentCulture <- culture
        Thread.CurrentThread.CurrentUICulture <- culture

        Log.Logger <- LoggerConfiguration()
            .WriteTo.File("csharp-ls.log")
            .CreateLogger()
        
        let argParser = ArgumentParser.Create<Options.CLIArguments>(programName = "csharp-ls")
        let serverArgs = argParser.Parse args

        serverArgs.TryGetResult(<@ Options.CLIArguments.Version @>)
            |> Option.iter (fun _ -> printfn "csharp-ls, %s"
                                             (Assembly.GetExecutingAssembly().GetName().Version |> string)
                                     exit 0)

        let parseLogLevel (s: string) =
            match s.ToLowerInvariant() with
            | "error" -> Ionide.LanguageServerProtocol.Types.MessageType.Error
            | "warning" -> Ionide.LanguageServerProtocol.Types.MessageType.Warning
            | "info" -> Ionide.LanguageServerProtocol.Types.MessageType.Info
            | "log" -> Ionide.LanguageServerProtocol.Types.MessageType.Log
            | _ -> Ionide.LanguageServerProtocol.Types.MessageType.Log

        // default the verbosity to warning
        let settings: ServerSettings = {
            SolutionPath = serverArgs.TryGetResult(<@ Options.CLIArguments.Solution @>)
            LogLevel = serverArgs.TryGetResult(<@ Options.CLIArguments.LogLevel @>)
                       |> Option.defaultValue "log"
                       |> parseLogLevel
        }
        
        Log.Information($"csharp-ls settings solution: {settings.SolutionPath} log level {settings.LogLevel}")
        Server.start settings
    with
    | :? ArguParseException as ex ->
        Log.Error(ex, "csharp-ls error during parsing")
        printfn "%s" ex.Message

        match ex.ErrorCode with
        | ErrorCode.HelpText -> 0
        | _ ->
            Log.Error(ex, "csharp-ls Unrecognised arguments")
            1  // Unrecognised arguments

    | e ->
        Log.Error(e, "csharp-ls crashed")
        printfn "Server crashing error - %s \n %s" e.Message e.StackTrace
        3
