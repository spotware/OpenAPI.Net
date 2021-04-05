using Google.Protobuf;
using OpenAPI.Net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Reactive.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinForms.Demo
{
    public partial class Main : Form
    {
        private string _clientId = "";
        private string _clientSecret = "";
        private string _token = "";
        private string _apiHost = "demo.ctraderapi.com";

        private int _apiPort = 5035;
        private long _accountID = 15084071;

        //private string _clientId = "185_Cmy5vh47ORewO95NsLCbz10Xn6RAzxFA13fgyg5xTKhzuxj7jr";
        //private string _clientSecret = "JcE4vc5TvscRmtouoqMZS8TxJ31119beYt1TInP4tnOqhCBHL9";
        //private string _token = "bCLrebRzU3Lhq3eXf6y7-Xls6yfVAJNqUwLFk0__yoM";
        //private string _apiHost = "sandbox-tradeapi.spotware.com";
        // private int _accountID = 102741;
        //  private int _apiPort = 5035;
        private readonly OpenClient _client;

        private static Queue _writeQueue = new Queue(); // not thread safe
        private static Queue _readQueue = new Queue(); // not thread safe
        private static Queue writeQueueSync = Queue.Synchronized(_writeQueue); // thread safe
        private static Queue readQueueSync = Queue.Synchronized(_readQueue); // thread safe
        private static volatile bool isShutdown;
        private static volatile bool isRestart;
        private static int MaxMessageSize = 1000000;
        private static long testOrderId = -1;
        private static long testPositionId = -1;
        private IList<ProtoOACtidTraderAccount> _accounts;
        private List<ProtoOATrader> _traders;

        public Main()
        {
            _client = new OpenClient(_apiHost, _apiPort, TimeSpan.FromSeconds(10));

            _client.ObserveOn(SynchronizationContext.Current).OfType<ProtoMessage>().Subscribe(OnMessage, OnError);
            _client.ObserveOn(SynchronizationContext.Current).OfType<ProtoOAExecutionEvent>().Subscribe(OnExecutionEvent);
            _client.ObserveOn(SynchronizationContext.Current).OfType<ProtoOAGetAccountListByAccessTokenRes>().Subscribe(OnAccountListResponse);
            _client.ObserveOn(SynchronizationContext.Current).OfType<ProtoOATraderRes>().Subscribe(OnTraderResponse);

            InitializeComponent();
        }

        private void OnTraderResponse(ProtoOATraderRes response)
        {
            _traders.Add(response.Trader);
        }

        private void OnAccountListResponse(ProtoOAGetAccountListByAccessTokenRes response)
        {
            _accounts = response.CtidTraderAccount;
        }

        private void OnExecutionEvent(ProtoOAExecutionEvent executionEvent)
        {
            testOrderId = executionEvent.Order.OrderId;

            testPositionId = executionEvent.Position.PositionId;
        }

        private void OnMessage(ProtoMessage message)
        {
            readQueueSync.Enqueue("Received: " + OpenApiMessagesPresentation.ToString(message));
        }

        private void OnError(Exception exception)
        {
            MessageBox.Show(exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            Close();
        }

        private async void Transmit(ProtoMessage prtoMessage, bool log = true)
        {
            if (log)
            {
                txtMessages.Text += "Send: " + OpenApiMessagesPresentation.ToString(prtoMessage);
                txtMessages.Text += Environment.NewLine;
            }

            if (string.IsNullOrWhiteSpace(prtoMessage.ClientMsgId)) prtoMessage.ClientMsgId = "Client Message";

            await _client.SendMessage(prtoMessage);
        }

        private void btnAuthorizeApplication_Click(object sender, EventArgs e)
        {
            var protoMessage = new ProtoMessage
            {
                Payload = new ProtoOAApplicationAuthReq
                {
                    ClientId = _clientId,
                    ClientSecret = _clientSecret,
                }.ToByteString(),
                PayloadType = (int)ProtoOAPayloadType.ProtoOaApplicationAuthReq
            };

            Transmit(protoMessage);
        }

        private async void Main_Load(object sender, EventArgs e)
        {
            await _client.Connect();
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;
            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);
            return false;
        }

        private async void btnGetAccountsList_Click(object sender, EventArgs e)
        {
            var protoMessage = new ProtoMessage
            {
                Payload = new ProtoOAGetAccountListByAccessTokenReq
                {
                    AccessToken = _token
                }.ToByteString(),
                PayloadType = (int)ProtoOAPayloadType.ProtoOaGetAccountsByAccessTokenReq
            };

            Transmit(protoMessage);

            while (_accounts == null)
            {
                await Task.Delay(50);
            }
            _traders = new List<ProtoOATrader>();
            foreach (var account in _accounts)
            {
                if (!account.IsLive)
                {
                    var accountAuthRequest = new ProtoMessage
                    {
                        Payload = new ProtoOAAccountAuthReq
                        {
                            AccessToken = _token,
                            CtidTraderAccountId = (long)account.CtidTraderAccountId,
                        }.ToByteString(),
                        PayloadType = (int)ProtoOAPayloadType.ProtoOaAccountAuthReq
                    };

                    Transmit(accountAuthRequest);

                    await Task.Delay(500);

                    var traderRequest = new ProtoMessage
                    {
                        Payload = new ProtoOATraderReq
                        {
                            CtidTraderAccountId = (long)account.CtidTraderAccountId,
                        }.ToByteString(),
                        PayloadType = (int)ProtoOAPayloadType.ProtoOaTraderReq
                    };

                    Transmit(traderRequest);
                }
            }
            await Task.Delay(1000);

            foreach (var trader in _traders)
            {
                cbAccounts.Items.Add(trader.CtidTraderAccountId);
            }
            _accounts = null;
        }

        private void btnAuthorizeAccount_Click(object sender, EventArgs e)
        {
            var message = new ProtoMessage
            {
                Payload = new ProtoOAAccountAuthReq
                {
                    AccessToken = _token,
                    CtidTraderAccountId = _accountID,
                }.ToByteString(),
                PayloadType = (int)ProtoOAPayloadType.ProtoOaAccountAuthReq
            };

            Transmit(message);
        }

        private void btnSendMarketOrder_Click(object sender, EventArgs e)
        {
            var message = new ProtoMessage
            {
                Payload = new ProtoOANewOrderReq
                {
                    CtidTraderAccountId = _accountID,
                    SymbolId = 1,
                    TradeSide = ProtoOATradeSide.Buy,
                    Volume = 100000,
                    OrderType = ProtoOAOrderType.Market
                }.ToByteString(),
                PayloadType = (int)ProtoOAPayloadType.ProtoOaNewOrderReq
            };

            Transmit(message);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (_readQueue.Count > 0)
            {
                txtMessages.AppendText((string)_readQueue.Dequeue() + Environment.NewLine);
            }
        }

        private void btnSubscribeForSpots_Click(object sender, EventArgs e)
        {
            var spotRequest = new ProtoOASubscribeSpotsReq
            {
                CtidTraderAccountId = _accountID,
            };

            spotRequest.SymbolId.Add(1);

            var message = new ProtoMessage
            {
                Payload = spotRequest.ToByteString(),
                PayloadType = (int)ProtoOAPayloadType.ProtoOaSubscribeSpotsReq
            };

            Transmit(message);
        }

        private void btnUnSubscribeFromSpots_Click(object sender, EventArgs e)
        {
            var spotRequest = new ProtoOAUnsubscribeSpotsReq
            {
                CtidTraderAccountId = _accountID,
            };

            spotRequest.SymbolId.Add(1);

            var message = new ProtoMessage
            {
                Payload = spotRequest.ToByteString(),
                PayloadType = (int)ProtoOAPayloadType.ProtoOaUnsubscribeSpotsReq
            };

            Transmit(message);
        }

        private void btnGetDealsList_Click(object sender, EventArgs e)
        {
            var message = new ProtoMessage
            {
                Payload = new ProtoOADealListReq
                {
                    CtidTraderAccountId = _accountID,
                    FromTimestamp = DateTimeOffset.Now.AddDays(-7).ToUnixTimeMilliseconds(),
                    ToTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
                }.ToByteString(),
                PayloadType = (int)ProtoOAPayloadType.ProtoOaDealListReq
            };

            Transmit(message);
        }

        private void btnGetOrders_Click(object sender, EventArgs e)
        {
            var message = new ProtoMessage
            {
                Payload = new ProtoOAReconcileReq
                {
                    CtidTraderAccountId = _accountID
                }.ToByteString(),
                PayloadType = (int)ProtoOAPayloadType.ProtoOaReconcileReq
            };

            Transmit(message);
        }

        private void btnGetTransactions_Click(object sender, EventArgs e)
        {
            var message = new ProtoMessage
            {
                Payload = new ProtoOACashFlowHistoryListReq
                {
                    CtidTraderAccountId = _accountID,
                    FromTimestamp = DateTimeOffset.Now.AddDays(-7).ToUnixTimeMilliseconds(),
                    ToTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
                }.ToByteString(),
                PayloadType = (int)ProtoOAPayloadType.ProtoOaCashFlowHistoryListReq
            };

            Transmit(message);
        }

        private void btnGetTrendbars_Click(object sender, EventArgs e)
        {
            var message = new ProtoMessage
            {
                Payload = new ProtoOAGetTrendbarsReq
                {
                    CtidTraderAccountId = _accountID,
                    SymbolId = 1,
                    Period = ProtoOATrendbarPeriod.M1,
                    FromTimestamp = DateTimeOffset.Now.AddDays(-35).ToUnixTimeMilliseconds(),
                    ToTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
                }.ToByteString(),
                PayloadType = (int)ProtoOAPayloadType.ProtoOaGetTrendbarsReq
            };

            Transmit(message);
        }

        private void btnGetTickData_Click(object sender, EventArgs e)
        {
            var message = new ProtoMessage
            {
                Payload = new ProtoOAGetTickDataReq
                {
                    CtidTraderAccountId = _accountID,
                    SymbolId = 1,
                    Type = ProtoOAQuoteType.Bid,
                    FromTimestamp = DateTimeOffset.Now.AddDays(-35).ToUnixTimeMilliseconds(),
                    ToTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
                }.ToByteString(),
                PayloadType = (int)ProtoOAPayloadType.ProtoOaGetTickdataReq,
                ClientMsgId = "EURUSD"
            };

            Transmit(message);
        }

        private void btnGetSymbols_Click(object sender, EventArgs e)
        {
            var message = new ProtoMessage
            {
                Payload = new ProtoOASymbolsListReq
                {
                    CtidTraderAccountId = _accountID,
                }.ToByteString(),
                PayloadType = (int)ProtoOAPayloadType.ProtoOaSymbolsListReq,
            };

            Transmit(message);
        }

        private void btnGetProfile_Click(object sender, EventArgs e)
        {
        }

        private void btnSendStopOrder_Click(object sender, EventArgs e)
        {
            var message = new ProtoMessage
            {
                Payload = new ProtoOANewOrderReq
                {
                    CtidTraderAccountId = _accountID,
                    SymbolId = 1,
                    TradeSide = ProtoOATradeSide.Buy,
                    Volume = 100000,
                    OrderType = ProtoOAOrderType.Stop,
                    StopPrice = 1.2
                }.ToByteString(),
                PayloadType = (int)ProtoOAPayloadType.ProtoOaNewOrderReq,
            };

            Transmit(message);
        }

        private void btnSendLimitOrder_Click(object sender, EventArgs e)
        {
            var message = new ProtoMessage
            {
                Payload = new ProtoOANewOrderReq
                {
                    CtidTraderAccountId = _accountID,
                    SymbolId = 1,
                    TradeSide = ProtoOATradeSide.Buy,
                    Volume = 100000,
                    OrderType = ProtoOAOrderType.Limit,
                    LimitPrice = 1.1
                }.ToByteString(),
                PayloadType = (int)ProtoOAPayloadType.ProtoOaNewOrderReq,
            };

            Transmit(message);
        }

        private void btnSendStopLimitOrder_Click(object sender, EventArgs e)
        {
            var message = new ProtoMessage
            {
                Payload = new ProtoOANewOrderReq
                {
                    CtidTraderAccountId = _accountID,
                    SymbolId = 1,
                    TradeSide = ProtoOATradeSide.Buy,
                    Volume = 100000,
                    OrderType = ProtoOAOrderType.StopLimit,
                    LimitPrice = 1.1,
                    SlippageInPoints = 5
                }.ToByteString(),
                PayloadType = (int)ProtoOAPayloadType.ProtoOaNewOrderReq,
            };

            Transmit(message);
        }

        private void btnClosePosition_Click(object sender, EventArgs e)
        {
            var message = new ProtoMessage
            {
                Payload = new ProtoOAClosePositionReq
                {
                    CtidTraderAccountId = _accountID,
                    PositionId = Convert.ToInt64(txtPositionID.Text),
                    Volume = Convert.ToInt64(txtVolume.Text),
                }.ToByteString(),
                PayloadType = (int)ProtoOAPayloadType.ProtoOaClosePositionReq,
            };

            Transmit(message);
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtMessages.Text = string.Empty;
        }

        private void btnAmentSLTP_Click(object sender, EventArgs e)
        {
            var message = new ProtoMessage
            {
                Payload = new ProtoOAAmendPositionSLTPReq
                {
                    CtidTraderAccountId = _accountID,
                    PositionId = Convert.ToInt64(txtPositionIDTPSL.Text),
                    StopLoss = Convert.ToDouble(txtStopLoss.Text),
                    TakeProfit = Convert.ToDouble(txtTakeProfit.Text)
                }.ToByteString(),
                PayloadType = (int)ProtoOAPayloadType.ProtoOaClosePositionReq,
            };

            Transmit(message);
        }

        private void cbAccounts_SelectedIndexChanged(object sender, EventArgs e)
        {
            _accountID = (long)cbAccounts.SelectedItem;
        }
    }
}