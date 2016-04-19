//The MIT License (MIT)
//
//Copyright (c) 2016
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.
module FSLogger

open System
open System.IO

type LogLevel = 
    | Debug = 0
    | Info = 1
    | Warn = 2
    | Error = 3
    | Fatal = 4

/// Immutable struct holding information relating to a log entry.
[<Struct>]
type LogEntry(level : LogLevel, time : DateTime, path : string, message : string) = 
    
    /// The log level of the message
    member __.Level = level
    
    /// The time at which the entry was logged
    member __.Time = time
    
    /// The source of the message
    member __.Path = path
    
    /// The actual log message
    member __.Message = message
    
    override __.ToString() = sprintf "[%A|%A]%s :%s" DateTime.Now level path message

/// Immmutable logger, which holds information about the logging context.
[<Struct>]
type Logger internal (path : string, consumer : LogEntry -> unit) = 
    
    /// The current path of this logger
    member __.Path = path
    
    /// The current consumer for this logger
    member __.Consumer = consumer
    
    /// Logs an unformatted message at the specified level
    member private __.Log level message = 
        let logEntry = LogEntry(level, DateTime.Now, path, message)
        consumer logEntry
    
    /// Logs the message at the specified level
    member x.Logf level format = Printf.ksprintf (x.Log level) format
    
    override __.ToString() = sprintf "Logger: {path = '%s'; consumers = %A}" path consumer

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Logger = 
    /// The default logger. Has no path and does nothing on consumption.
    let Default = Logger("", ignore)
    
    /// A logger that prints to std::out
    let Printfn = Logger("", printfn "%A")
    
    /// Creates a new logger with the provided consumer
    let withConsumer newConsumer (logger : Logger) = Logger(logger.Path, newConsumer)
    
    /// Creates a new logger with the provided path
    let withPath newPath (logger : Logger) = Logger(newPath, logger.Consumer)
    
    /// Creates a new logger with the provided path appended. This is useful for heirarchical logger pathing.
    let appendPath newPath (logger : Logger) = Logger(Path.Combine(logger.Path, newPath), logger.Consumer)
    
    /// Logs a message to the logger at the provided level.
    let inline logf level (logger : Logger) format = logger.Logf level format
    
    /// Adds a consumer to the logger, such that the new and current consumers are run.
    let addConsumer newConsumer (logger : Logger) = 
        let curConsumer = logger.Consumer
        
        let consume l = 
            curConsumer l
            newConsumer l
        logger |> withConsumer consume
    
    /// Creates a new logger with a mapping function over the log entries.
    let decorate f (logger : Logger) = Logger(logger.Path, f >> logger.Consumer)
    
    /// Creates a new logger that indents all messages in the logger by 4 spaces.
    let indent : Logger -> Logger = 
        let indentF (l : LogEntry) = LogEntry(l.Level, l.Time, l.Path, sprintf "    %s" l.Message)
        decorate indentF