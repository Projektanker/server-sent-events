# Server-Sent Events
Server-Sent Events implementation for C#

[![CI / CD](https://github.com/Projektanker/server-sent-events/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/Projektanker/server-sent-events/actions/workflows/ci-cd.yml)

## NuGet
| Name | Description | Version |
|:-|:-|:-|
| [Projektanker.ServerSentEvents](https://www.nuget.org/packages/Projektanker.ServerSentEvents/) | Core library | ![Nuget](https://img.shields.io/nuget/v/Projektanker.ServerSentEvents) |
| [Projektanker.ServerSentEvents.Client](https://www.nuget.org/packages/Projektanker.ServerSentEvents.Client/) | Client (EventSource) | ![Nuget](https://img.shields.io/nuget/v/Projektanker.ServerSentEvents.Client) |
| [Projektanker.ServerSentEvents.Server](https://www.nuget.org/packages/Projektanker.ServerSentEvents.Server/) | Server  (ServerSentEventsWriter) | ![Nuget](https://img.shields.io/nuget/v/Projektanker.ServerSentEvents.Server) |
| [Projektanker.ServerSentEvents.AspNetCore.Mvc](https://www.nuget.org/packages/Projektanker.ServerSentEvents.AspNetCore.Mvc/) | ASP&period;NET Core (ServerSentEventsActionResult) | ![Nuget](https://img.shields.io/nuget/v/Projektanker.ServerSentEvents.AspNetCore.Mvc) |

## EventSource
C# implementation of [the EventSource-interface](https://html.spec.whatwg.org/multipage/server-sent-events.html#the-eventsource-interface)

## ServerSentEventsActionResult
Server side ActionResult to send events from server to client.
