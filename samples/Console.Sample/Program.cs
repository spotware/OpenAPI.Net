using Google.Protobuf;
using OpenAPI.Net;
using OpenAPI.Net.Auth;
using OpenAPI.Net.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace ConsoleDemo
{
    internal class Program
    {
        private static App _app;

        private static Token _token;

        private static OpenClient _client;

        private static readonly List<IDisposable> _disposables = new();

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

                _token = new Token
                {
                    AccessToken = accessToken
                };

                _app = new App(appId, appSecret, string.Empty);
            }
            else
            {
                Console.Write("Enter App Redirect URL: ");

                var redirectUrl = Console.ReadLine();

                _app = new App(appId, appSecret, redirectUrl);

                Console.Write("Enter Scope (Trading or Accounts): ");

                var scopeString = Console.ReadLine();

                var scope = (Scope)Enum.Parse(typeof(Scope), scopeString, true);

                var authUri = _app.GetAuthUri();

                ShowDashLine();

                Console.WriteLine($"Authentication URI: {authUri}");

                OpenBrowser(authUri);

                Console.WriteLine("Follow the authentication steps on your browser, then copy the authentication code from redirect" +
                    " URL and paste it here.");

                Console.WriteLine("The authentication code is at the end of redirect URL and it starts after '?code=' parameter.");

                ShowDashLine();

                Console.Write("Enter Authentication Code: ");

                var authCode = Console.ReadLine();

                _token = await TokenFactory.GetToken(authCode, _app);

                ShowDashLine();

                Console.WriteLine($"Access token generated: {_token.AccessToken}");
            }

            var host = ApiInfo.GetHost(mode);

            _client = new OpenClient(host, ApiInfo.Port, TimeSpan.FromSeconds(10), useWebSocket: useWebScoket);

            _disposables.Add(_client.Where(iMessage => iMessage is not ProtoHeartbeatEvent).Subscribe(OnMessageReceived, OnException));
            _disposables.Add(_client.OfType<ProtoOARefreshTokenRes>().Subscribe(OnRefreshTokenResponse));

            Console.WriteLine("Connecting Client...");

            await _client.Connect();

            ShowDashLine();

            Console.WriteLine("Client successfully connected");

            ShowDashLine();

            Console.WriteLine("Sending App Auth Req...");

            Console.WriteLine("Please wait...");

            ShowDashLine();

            var applicationAuthReq = new ProtoOAApplicationAuthReq
            {
                ClientId = _app.ClientId,
                ClientSecret = _app.Secret,
            };

            await _client.SendMessage(applicationAuthReq);

            await Task.Delay(5000);

            Console.WriteLine("You should see the application auth response message before entering any command");

            Console.WriteLine("For commands list and description use 'help' command");

            ShowDashLine();

            GetCommand();
        }

        private static void OnMessageReceived(IMessage message)
        {
            Console.WriteLine($"\nMessage Received:\n{message}");

            Console.WriteLine();
        }

        private static void OnException(Exception ex)
        {
            Console.WriteLine($"\nException\n: {ex}");

            ShowDashLine();
        }

        private static void OnRefreshTokenResponse(ProtoOARefreshTokenRes response)
        {
            _token = new Token
            {
                AccessToken = response.AccessToken,
                RefreshToken = response.RefreshToken,
                ExpiresIn = DateTimeOffset.FromUnixTimeMilliseconds(response.ExpiresIn),
                TokenType = response.TokenType,
            };

            Console.WriteLine($"New token received: {_token.AccessToken}");
            Console.WriteLine($"As you refreshed your access token, now you have to re-authorize all previously authorized" +
                $" trading accounts");
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

                        var trendbars = Enum.GetValues(typeof(ProtoOATrendbarPeriod)).Cast<ProtoOATrendbarPeriod>();

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
            if (string.IsNullOrWhiteSpace(_token.RefreshToken))
            {
                Console.Write("Refresh Token: ");

                _token.RefreshToken = Console.ReadLine();
            }

            Console.WriteLine("Sending ProtoOARefreshTokenReq...");

            var request = new ProtoOARefreshTokenReq
            {
                RefreshToken = _token.RefreshToken
            };

            await _client.SendMessage(request);
        }

        private static async void SubscribeToSymbolTrendBar(string[] commandSplit)
        {
            Console.WriteLine("Sending ProtoOASubscribeLiveTrendbarReq...");

            var request = new ProtoOASubscribeLiveTrendbarReq()
            {
                Period = (ProtoOATrendbarPeriod)Enum.Parse(typeof(ProtoOATrendbarPeriod), commandSplit[2], true),
                CtidTraderAccountId = long.Parse(commandSplit[3]),
                SymbolId = long.Parse(commandSplit[4]),
            };

            await _client.SendMessage(request);
        }

        private static async void SubscribeToSymbolSpot(string[] commandSplit)
        {
            Console.WriteLine("Sending ProtoOASubscribeSpotsReq...");

            var request = new ProtoOASubscribeSpotsReq()
            {
                CtidTraderAccountId = long.Parse(commandSplit[2]),
            };

            request.SymbolId.AddRange(commandSplit.Skip(3).Select(iSymbolId => long.Parse(iSymbolId)));

            await _client.SendMessage(request);
        }

        private static async void SymbolListRequest(string[] commandSplit)
        {
            var accountId = long.Parse(commandSplit[1]);

            Console.WriteLine("Sending ProtoOASymbolsListReq...");

            var request = new ProtoOASymbolsListReq
            {
                CtidTraderAccountId = accountId,
            };

            await _client.SendMessage(request);
        }

        private static async void ReconcileRequest(string[] commandSplit)
        {
            var accountId = long.Parse(commandSplit[1]);

            Console.WriteLine("Sending ProtoOAReconcileReq...");

            var request = new ProtoOAReconcileReq
            {
                CtidTraderAccountId = accountId,
            };

            await _client.SendMessage(request);
        }

        private static async void AccountListRequest()
        {
            Console.WriteLine("Sending ProtoOAGetAccountListByAccessTokenReq...");

            var request = new ProtoOAGetAccountListByAccessTokenReq
            {
                AccessToken = _token.AccessToken,
            };

            await _client.SendMessage(request);
        }

        private static async void AccountAuthRequest(string[] commandSplit)
        {
            var accountId = long.Parse(commandSplit[1]);

            Console.WriteLine("Sending ProtoOAAccountAuthReq...");

            var request = new ProtoOAAccountAuthReq
            {
                CtidTraderAccountId = accountId,
                AccessToken = _token.AccessToken
            };

            await _client.SendMessage(request);
        }

        private static async void TickDataRequest(string[] commandSplit)
        {
            var accountId = long.Parse(commandSplit[1]);
            var symbolId = long.Parse(commandSplit[2]);
            var type = commandSplit[3].ToLowerInvariant() switch
            {
                "bid" => ProtoOAQuoteType.Bid,
                _ => ProtoOAQuoteType.Ask
            };

            var hours = long.Parse(commandSplit[4]);

            Console.WriteLine("Sending ProtoOAGetTickDataReq...");

            var request = new ProtoOAGetTickDataReq
            {
                CtidTraderAccountId = accountId,
                SymbolId = symbolId,
                FromTimestamp = DateTimeOffset.UtcNow.AddHours(-hours).ToUnixTimeMilliseconds(),
                ToTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Type = type
            };

            await _client.SendMessage(request);
        }

        private static async void TrendbarRequest(string[] commandSplit)
        {
            var period = (ProtoOATrendbarPeriod)Enum.Parse(typeof(ProtoOATrendbarPeriod), commandSplit[1], true);
            var accountId = long.Parse(commandSplit[2]);
            var symbolId = long.Parse(commandSplit[3]);

            var days = long.Parse(commandSplit[4]);

            Console.WriteLine("Sending ProtoOAGetTrendbarsReq...");

            var request = new ProtoOAGetTrendbarsReq
            {
                CtidTraderAccountId = accountId,
                SymbolId = symbolId,
                Period = period,
                FromTimestamp = DateTimeOffset.UtcNow.AddDays(-days).ToUnixTimeMilliseconds(),
                ToTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            };

            Console.WriteLine($"FromTimestamp: {request.FromTimestamp} | ToTimestamp: {request.ToTimestamp}");

            await _client.SendMessage(request);
        }

        private static async void ProfileRequest()
        {
            Console.WriteLine("Sending ProtoOAGetCtidProfileByTokenReq...");

            var request = new ProtoOAGetCtidProfileByTokenReq
            {
                AccessToken = _token.AccessToken
            };

            await _client.SendMessage(request);
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

            _disposables.ForEach(iDisposable => iDisposable.Dispose());

            _client.Dispose();

            Console.WriteLine("Disconnected, exiting...");

            Task.Delay(3000).Wait();

            Environment.Exit(0);
        }

        public static void OpenBrowser(Uri url)
        {
            try
            {
                System.Diagnostics.Process.Start(url.AbsoluteUri);
            }
            catch
            {
                if (System.OperatingSystem.IsWindows())
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd", $"/c start {url.AbsoluteUri}") { CreateNoWindow = true });
                }
                else if (System.OperatingSystem.IsLinux())
                {
                    System.Diagnostics.Process.Start("xdg-open", url.AbsoluteUri);
                }
                else if (System.OperatingSystem.IsMacOS())
                {
                    System.Diagnostics.Process.Start("open", url.AbsoluteUri);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}