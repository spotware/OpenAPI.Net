# OpenAPI.Net

[![NuGet version (cTrader.OpenAPI.Net)](https://buildstats.info/nuget/cTrader.OpenAPI.Net)](https://www.nuget.org/packages/cTrader.OpenAPI.Net/)

cTrader Open API .NET Rx library (Experimental version!)

This library allows you to easily use and integrate cTrader Open API on your .NET applications.

Its written by using RX streams so it makes API usage very easy and allows you to do a lot with few lines of code.

It uses channels and array pools to avoid too many allocations, we tried our best to make it as efficient as possible.

Current version of library targets [.NET Standard 2.0](https://docs.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0#tabpanel_1_net-standard-2-0), so you can use it on .NET framework apps but in order to use [Reactive](https://github.com/dotnet/reactive) you may need to set your project's 'LangVersion' to a higher one (tested with 9.0)

Please check the samples, we have some good samples for all kinds of .NET apps.

Feel free to fork and improve it!

Documentation: [https://spotware.github.io/OpenAPI.Net/](https://spotware.github.io/OpenAPI.Net/)

## Dependencies

* [protobuf](https://github.com/protocolbuffers/protobuf)
* [Reactive](https://github.com/dotnet/reactive)
* [websocket-client](https://github.com/Marfusios/websocket-client)

## Licence

Licenced under the [MIT licence](LICENSE).
