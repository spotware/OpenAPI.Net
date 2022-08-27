using OpenAPI.Net;
using OpenAPI.Net.Auth;
using OpenAPI.Net.Helpers;
using ProtoOA.Enums;
using ProtoOA.Event;
using ProtoOA.Model;
using ProtoOA.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleDemo
{
    internal class Program
    {
        private static App app;

        private static Token token;

        private static OpenClient client;

        private static readonly List<IDisposable> disposables = new();

        private static async Task Main()
        {
            Console.Write("Enter App ID: ");

            var appId = Console.ReadLine();

            Console.Write("Enter App Secret: ");

            var appSecret = Console.ReadLine();

            Console.Write("Enter Client Type (WebSocket Or TCP): ");

            var useWebScoket = Console.ReadLine().ToLowerInvariant() switch
            {
                "websocket" => true,
                _ => false
            };

            Console.Write("Enter Connection Mode (Live or Demo): ");

            var modeString = Console.ReadLine();

            var mode = (Mode)Enum.Parse(typeof(Mode), modeString, true);

            Console.Write("Do you have an access token (Y/N): ");

            var isTokenAvailable = Console.ReadLine().ToLowerInvariant() switch
            {
                "y" => true,
                _ => false
            };

            if (isTokenAvailable)
            {
                Console.Write("Your Access Token: ");

                var accessToken = Console.ReadLine();

                token = new Token
                {
                    AccessToken = accessToken
                };

                app = new App(appId, appSecret, string.Empty);
            }
            else
            {
                Console.Write("Enter App Redirect URL: ");

                var redirectUrl = Console.ReadLine();

                app = new App(appId, appSecret, redirectUrl);

                Console.Write("Enter Scope (Trading or Accounts): ");

                var scopeString = Console.ReadLine();

                var scope = (Scope)Enum.Parse(typeof(Scope), scopeString, true);

                var authUri = app.GetAuthUri();

                ShowDashLine();

                Console.WriteLine($"Authentication URI: {authUri}");

                System.Diagnostics.Process.Start("explorer.exe", $"\"{authUri}\"");

                Console.WriteLine("Follow the authentication steps on your browser, then copy the authentication code from redirect" +
                    " URL and paste it here.");

                Console.WriteLine("The authentication code is at the end of redirect URL and it starts after '?code=' parameter.");

                ShowDashLine();

                Console.Write("Enter Authentication Code: ");

                var authCode = Console.ReadLine();

                token = await TokenFactory.GetToken(authCode, app);

                ShowDashLine();

                Console.WriteLine($"Access token generated: {token.AccessToken}");
            }

            var host = ApiInfo.GetHost(mode);

            client = new OpenClient(host, ApiInfo.Port, TimeSpan.FromSeconds(10), useWebSocket: useWebScoket);

            //Explicit IObservable's event hadling
            disposables.Add(client.SpotObservable.Subscribe(OnSpotEvent, OnSpotError));
            disposables.Add(client.ExecutionObservable.Subscribe(OnExecutionEvent, OnExecutionError));
            //Event handlers also work
            client.SpotEvent += client_SpotEvent;
            client.ExecutionEvent += client_ExecutionEvent;

            Console.WriteLine("Connecting Client...");

            if (!client.Connect().Wait(10000))
            {
                Console.WriteLine("Connection timeout...");
                System.Environment.Exit(1);
            }

            ShowDashLine();

            Console.WriteLine("Client successfully connected");

            ShowDashLine();

            Console.WriteLine("Sending App Auth Req...");

            Console.WriteLine("Please wait...");

            ShowDashLine();

            var applicationAuthRes = await client.ApplicationAuthorize(app.ClientId, app.Secret);
            //var applicationAuthRes = await client.ApplicationAuthorize(app);
            Console.WriteLine(applicationAuthRes.ToString());

            Console.WriteLine("For commands list and description use 'help' command");

            ShowDashLine();

            GetCommand();
        }

        private static async Task client_ExecutionEvent(object sender, OAEventArgs<ExecutionEvent> eventArgs)
        {
            Console.WriteLine("client_ExecutionEvent");
        }

        private static async Task client_SpotEvent(object sender, OAEventArgs<SpotEvent> eventArgs)
        {
            Console.WriteLine("client_SpotEvent");
        }

        private static void OnExecutionError(Exception ex)
        {
            Console.WriteLine($"\nException\n: {ex}");

            ShowDashLine();
        }

        private static void OnExecutionEvent(ExecutionEvent obj)
        {
            Console.WriteLine("Client_ExecutionEvent: " + obj.ToString());
        }
        private static void OnSpotError(Exception ex)
        {
            Console.WriteLine($"\nException\n: {ex}");
            ShowDashLine();
        }

        private static void OnSpotEvent(SpotEvent spot)
        {
            Console.WriteLine($"SpotEvent {spot.Timestamp} SymbolId={spot.SymbolId} Ask={spot.Ask} Bid={spot.Bid}");
        }

        private static void ProcessCommand(string command)
        {
            Console.WriteLine();

            var commandSplit = command.Split(' ');
            try
            {
                switch (commandSplit[0].ToLowerInvariant())
                {
                    case "help":
                        Console.WriteLine("For getting accounts list type: accountlist\n");
                        Console.WriteLine("For authorizing an account type: accountauth {Account ID}\n");
                        Console.WriteLine("For getting an account symbols list type (Requires account authorization): symbolslist {Account ID}\n");
                        Console.WriteLine("For subscribing to symbol(s) spot quotes type (Requires account authorization): subscribe spot {Account ID} {Symbol ID,}\n");
                        Console.WriteLine("For subscribing to symbol(s) trend bar type (Requires account authorization and spot subscription): subscribe trendbar {Period} {Account ID} {Symbol ID}\n");
                        Console.WriteLine("For getting trend bar data: trendbar {Period} {Account ID} {Symbol ID} {numberOfDays}\n");
                        Console.WriteLine("For trend bar period parameter, you can use these values:\n");

                        var trendbars = Enum.GetValues(typeof(ProtoOA.Enums.TrendbarPeriod)).Cast<ProtoOA.Enums.TrendbarPeriod>();

                        var isFirst = true;

                        foreach (var trendBar in trendbars)
                        {
                            Console.Write(isFirst ? $"{trendBar}" : $", {trendBar}");

                            if (isFirst) { isFirst = false; }
                        }

                        Console.WriteLine();

                        Console.WriteLine("\nFor getting tick data: tickdata {Account ID} {Symbol ID} {Type (bid/ask)} {Number of Hours}\n");

                        Console.WriteLine("For getting profile: profile\n");

                        Console.WriteLine("To refresh access token, type: refreshtoken\n");

                        Console.WriteLine("To exit the app and disconnect the client type: disconnect\n");

                        Console.WriteLine("Commands aren't case sensitive\n");

                        break;

                    case "accountlist":
                        AccountListRequest();
                        break;

                    case "reconcile":
                        ReconcileRequest(commandSplit);
                        break;

                    case "accountauth":
                        AccountAuthRequest(commandSplit);
                        break;

                    case "symbolslist":
                        SymbolListRequest(commandSplit);
                        break;

                    case "symbol":
                        SymbolByIdRequest(commandSplit);
                        break;

                    case "subscribe":
                        ProcessSubscriptionCommand(commandSplit);
                        break;

                    case "trendbar":
                        TrendbarRequest(commandSplit);
                        break;

                    case "tickdata":
                        TickDataRequest(commandSplit);
                        break;

                    case "profile":
                        ProfileRequest();
                        break;

                    case "refreshtoken":
                        RefreshToken();
                        break;

                    case "disconnect":
                        Disconnect();

                        break;

                    default:
                        Console.WriteLine($"'{command}' is not recognized as a command, please use help command to get all available commands list");
                        break;
                }
            }
            catch (Exception ex)
            {
                if (ex is FormatException || ex is IndexOutOfRangeException)
                {
                    Console.WriteLine(ex);
                }
                else
                {
                    throw;
                }
            }

            Task.Delay(3000).Wait();

            GetCommand();
        }

        private static void ProcessSubscriptionCommand(string[] commandSplit)
        {
            switch (commandSplit[1].ToLowerInvariant())
            {
                case "spot":
                    SubscribeToSymbolSpot(commandSplit);
                    break;

                case "trendbar":
                    SubscribeToSymbolTrendBar(commandSplit);
                    break;

                default:
                    Console.WriteLine($"'{commandSplit[1]}' is not recognized as a subscription command, please use help command to get all available commands list");
                    break;
            }
        }

        private static async void RefreshToken()
        {
            if (string.IsNullOrWhiteSpace(Program.token.RefreshToken))
            {
                Console.Write("Refresh Token: ");

                Program.token.RefreshToken = Console.ReadLine();
            }

            Console.WriteLine("Sending ProtoOARefreshTokenReq...");

            Token token2 = await client.RefreshToken(token);
            if (token2 != null)
            {
                token.AccessToken = token2.AccessToken;
                Console.WriteLine($"New token: {token2.AccessToken}");
            }
            Console.WriteLine(token2.ToString());
        }

        private static async void SubscribeToSymbolTrendBar(string[] commandSplit)
        {
            Console.WriteLine("Sending ProtoOASubscribeLiveTrendbarReq...");

            TrendbarPeriod Period = (TrendbarPeriod)Enum.Parse(typeof(TrendbarPeriod), commandSplit[2], true);
            long CtidTraderAccountId = long.Parse(commandSplit[3]);
            long SymbolId = long.Parse(commandSplit[4]);

            await client.SubscribeToLiveTrendbar(CtidTraderAccountId, SymbolId, Period);
        }

        private static async void SubscribeToSymbolSpot(string[] commandSplit)
        {
            Console.WriteLine("Sending ProtoOASubscribeSpotsReq...");
            long CtidTraderAccountId = long.Parse(commandSplit[2]);
            long[] SymbolIds = commandSplit.Skip(3).Select(iSymbolId => long.Parse(iSymbolId)).ToArray();

            ProtoOA.Response.SubscribeSpotsRes subscribeSpotsRes = await client.SubscribeToSpots(CtidTraderAccountId, SymbolIds);
            Console.WriteLine(subscribeSpotsRes.ToString());
        }

        private static async void SymbolListRequest(string[] commandSplit)
        {
            var accountId = long.Parse(commandSplit[1]);

            Console.WriteLine("Sending ProtoOASymbolsListReq...");

            ProtoOA.Response.SymbolsListRes symbolsListRes = await client.GetSymbolList(accountId);
            foreach (LightSymbol s in symbolsListRes.Symbol)
            {
                Console.WriteLine(s.ToString());
            }
        }

        private static async void ReconcileRequest(string[] commandSplit)
        {
            var accountId = long.Parse(commandSplit[1]);

            Console.WriteLine("Sending ProtoOAReconcileReq...");

            ProtoOA.Response.ReconcileRes reconcileRes = await client.GetOpenPositionsOrders(accountId);
            Console.WriteLine(reconcileRes.ToString());
        }

        private static async void AccountListRequest()
        {
            Console.WriteLine("Sending ProtoOAGetAccountListByAccessTokenReq...");
            CtidTraderAccount[] ctidTraderAccounts = await client.GetAccountList(token);
            foreach (var account in ctidTraderAccounts)
            {
                Console.WriteLine(account.ToString());
            }
        }

        private static async void AccountAuthRequest(string[] commandSplit)
        {
            var accountId = long.Parse(commandSplit[1]);

            Console.WriteLine("Sending ProtoOAAccountAuthReq...");

            ProtoOA.Response.AccountAuthRes accountAuthRes = await client.AccountAuthorize(accountId, token);
            Console.WriteLine(accountAuthRes.ToString());
        }

        private static async void TickDataRequest(string[] commandSplit)
        {
            var accountId = long.Parse(commandSplit[1]);
            var symbolId = long.Parse(commandSplit[2]);
            var type = commandSplit[3].ToLowerInvariant() switch
            {
                "bid" => QuoteType.Bid,
                _ => QuoteType.Ask
            };

            var hours = long.Parse(commandSplit[4]);

            Console.WriteLine("Sending ProtoOAGetTickDataReq...");
            DateTimeOffset FromTimestamp = DateTimeOffset.UtcNow.AddHours(-hours);
            DateTimeOffset ToTimestamp = DateTimeOffset.UtcNow;

            TickData[] tickDatas = await client.GetTickData(accountId, symbolId, FromTimestamp, ToTimestamp, type);
            foreach (var tickData in tickDatas)
            {
                Console.WriteLine(tickData.ToString());
            }
        }

        private static async void TrendbarRequest(string[] commandSplit)
        {
            var period = (TrendbarPeriod)Enum.Parse(typeof(TrendbarPeriod), commandSplit[1], true);
            var accountId = long.Parse(commandSplit[2]);
            var symbolId = long.Parse(commandSplit[3]);

            var days = long.Parse(commandSplit[4]);

            Console.WriteLine("Sending ProtoOAGetTrendbarsReq...");

            DateTimeOffset FromTimestamp = DateTimeOffset.UtcNow.AddDays(-days);
            DateTimeOffset ToTimestamp = DateTimeOffset.UtcNow;

            Console.WriteLine($"FromTimestamp: {FromTimestamp} | ToTimestamp: {ToTimestamp}");

            ProtoOA.Response.GetTrendbarsRes getTrendbarsRes = await client.GetTrendbars(accountId, symbolId, period, FromTimestamp, ToTimestamp);
            foreach (var tb in getTrendbarsRes.Trendbar)
            {
                Console.WriteLine(tb.ToString());
            }
        }
        private static async void SymbolByIdRequest(string[] commandSplit)
        {
            var accountId = long.Parse(commandSplit[1]);
            var symbolIds = commandSplit.Skip(1).Select(s => long.Parse(s)).ToList();
            Console.WriteLine("Sending ProtoOASymbolByIdReq...");
            ProtoOA.Response.SymbolByIdRes symbolByIdRes = await client.GetSymbolsFull(accountId, symbolIds);
            foreach (var symbol in symbolByIdRes.Symbol)
            {
                Console.WriteLine(symbol.ToString());
            }
        }

        private static async void ProfileRequest()
        {
            Console.WriteLine("Sending ProtoOAGetCtidProfileByTokenReq...");

            CtidProfile ctidProfile = await client.GetProfile(token);
            Console.WriteLine(ctidProfile.ToString());
        }

        private static void GetCommand()
        {
            Console.Write("Enter command: ");

            var command = Console.ReadLine();

            ProcessCommand(command);
        }

        private static void ShowDashLine() => Console.WriteLine("--------------------------------------------------");

        private static void Disconnect()
        {
            Console.WriteLine("Disconnecting...");

            disposables.ForEach(iDisposable => iDisposable.Dispose());

            client.Dispose();

            Console.WriteLine("Disconnected, exiting...");

            Task.Delay(3000).Wait();

            Environment.Exit(0);
        }
    }
}