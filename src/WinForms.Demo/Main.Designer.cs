
namespace WinForms.Demo
{
    partial class Main
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.txtMessages = new System.Windows.Forms.TextBox();
            this.btnAuthorizeApplication = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.btnGetAccountsList = new System.Windows.Forms.Button();
            this.btnSendMarketOrder = new System.Windows.Forms.Button();
            this.btnSubscribeForSpots = new System.Windows.Forms.Button();
            this.btnUnSubscribeFrorSpots = new System.Windows.Forms.Button();
            this.btnGetDealsList = new System.Windows.Forms.Button();
            this.btnGetOrders = new System.Windows.Forms.Button();
            this.btnGetTransactions = new System.Windows.Forms.Button();
            this.btnGetTrendbars = new System.Windows.Forms.Button();
            this.btnGetTickData = new System.Windows.Forms.Button();
            this.btnGetSymbols = new System.Windows.Forms.Button();
            this.btnSendStopOrder = new System.Windows.Forms.Button();
            this.btnSendLimitOrder = new System.Windows.Forms.Button();
            this.btnSendStopLimitOrder = new System.Windows.Forms.Button();
            this.btnClosePosition = new System.Windows.Forms.Button();
            this.txtPositionID = new System.Windows.Forms.TextBox();
            this.txtVolume = new System.Windows.Forms.TextBox();
            this.lblID = new System.Windows.Forms.Label();
            this.lblVolume = new System.Windows.Forms.Label();
            this.btnClear = new System.Windows.Forms.Button();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.lblAccounts = new System.Windows.Forms.Label();
            this.cbAccounts = new System.Windows.Forms.ComboBox();
            this.lblTakeProfit = new System.Windows.Forms.Label();
            this.txtTakeProfit = new System.Windows.Forms.TextBox();
            this.lblStolLoss = new System.Windows.Forms.Label();
            this.lblID2 = new System.Windows.Forms.Label();
            this.txtStopLoss = new System.Windows.Forms.TextBox();
            this.txtPositionIDTPSL = new System.Windows.Forms.TextBox();
            this.btnAmentSLTP = new System.Windows.Forms.Button();
            this.refreshTokenButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtMessages
            // 
            this.txtMessages.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtMessages.Location = new System.Drawing.Point(0, 0);
            this.txtMessages.Margin = new System.Windows.Forms.Padding(4);
            this.txtMessages.Multiline = true;
            this.txtMessages.Name = "txtMessages";
            this.txtMessages.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtMessages.Size = new System.Drawing.Size(1581, 339);
            this.txtMessages.TabIndex = 0;
            // 
            // btnAuthorizeApplication
            // 
            this.btnAuthorizeApplication.Location = new System.Drawing.Point(19, 18);
            this.btnAuthorizeApplication.Margin = new System.Windows.Forms.Padding(4);
            this.btnAuthorizeApplication.Name = "btnAuthorizeApplication";
            this.btnAuthorizeApplication.Size = new System.Drawing.Size(193, 28);
            this.btnAuthorizeApplication.TabIndex = 1;
            this.btnAuthorizeApplication.Text = "Authorize Application";
            this.btnAuthorizeApplication.UseVisualStyleBackColor = true;
            this.btnAuthorizeApplication.Click += new System.EventHandler(this.btnAuthorizeApplication_Click);
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // btnGetAccountsList
            // 
            this.btnGetAccountsList.Location = new System.Drawing.Point(19, 54);
            this.btnGetAccountsList.Margin = new System.Windows.Forms.Padding(4);
            this.btnGetAccountsList.Name = "btnGetAccountsList";
            this.btnGetAccountsList.Size = new System.Drawing.Size(193, 28);
            this.btnGetAccountsList.TabIndex = 2;
            this.btnGetAccountsList.Text = "Get Accounts List";
            this.btnGetAccountsList.UseVisualStyleBackColor = true;
            this.btnGetAccountsList.Click += new System.EventHandler(this.btnGetAccountsList_Click);
            // 
            // btnSendMarketOrder
            // 
            this.btnSendMarketOrder.Location = new System.Drawing.Point(220, 90);
            this.btnSendMarketOrder.Margin = new System.Windows.Forms.Padding(4);
            this.btnSendMarketOrder.Name = "btnSendMarketOrder";
            this.btnSendMarketOrder.Size = new System.Drawing.Size(212, 28);
            this.btnSendMarketOrder.TabIndex = 4;
            this.btnSendMarketOrder.Text = "Send Market Order";
            this.btnSendMarketOrder.UseVisualStyleBackColor = true;
            this.btnSendMarketOrder.Click += new System.EventHandler(this.btnSendMarketOrder_Click);
            // 
            // btnSubscribeForSpots
            // 
            this.btnSubscribeForSpots.Location = new System.Drawing.Point(220, 18);
            this.btnSubscribeForSpots.Margin = new System.Windows.Forms.Padding(4);
            this.btnSubscribeForSpots.Name = "btnSubscribeForSpots";
            this.btnSubscribeForSpots.Size = new System.Drawing.Size(212, 28);
            this.btnSubscribeForSpots.TabIndex = 5;
            this.btnSubscribeForSpots.Text = "Subscribe For Spots";
            this.btnSubscribeForSpots.UseVisualStyleBackColor = true;
            this.btnSubscribeForSpots.Click += new System.EventHandler(this.btnSubscribeForSpots_Click);
            // 
            // btnUnSubscribeFrorSpots
            // 
            this.btnUnSubscribeFrorSpots.Location = new System.Drawing.Point(219, 54);
            this.btnUnSubscribeFrorSpots.Margin = new System.Windows.Forms.Padding(4);
            this.btnUnSubscribeFrorSpots.Name = "btnUnSubscribeFrorSpots";
            this.btnUnSubscribeFrorSpots.Size = new System.Drawing.Size(213, 28);
            this.btnUnSubscribeFrorSpots.TabIndex = 6;
            this.btnUnSubscribeFrorSpots.Text = "Unsubscribe From Spots";
            this.btnUnSubscribeFrorSpots.UseVisualStyleBackColor = true;
            this.btnUnSubscribeFrorSpots.Click += new System.EventHandler(this.btnUnSubscribeFromSpots_Click);
            // 
            // btnGetDealsList
            // 
            this.btnGetDealsList.Location = new System.Drawing.Point(16, 161);
            this.btnGetDealsList.Margin = new System.Windows.Forms.Padding(4);
            this.btnGetDealsList.Name = "btnGetDealsList";
            this.btnGetDealsList.Size = new System.Drawing.Size(193, 28);
            this.btnGetDealsList.TabIndex = 7;
            this.btnGetDealsList.Text = "Get Deals";
            this.btnGetDealsList.UseVisualStyleBackColor = true;
            this.btnGetDealsList.Click += new System.EventHandler(this.btnGetDealsList_Click);
            // 
            // btnGetOrders
            // 
            this.btnGetOrders.Location = new System.Drawing.Point(16, 197);
            this.btnGetOrders.Margin = new System.Windows.Forms.Padding(4);
            this.btnGetOrders.Name = "btnGetOrders";
            this.btnGetOrders.Size = new System.Drawing.Size(193, 28);
            this.btnGetOrders.TabIndex = 8;
            this.btnGetOrders.Text = "Get Orders";
            this.btnGetOrders.UseVisualStyleBackColor = true;
            this.btnGetOrders.Click += new System.EventHandler(this.btnGetOrders_Click);
            // 
            // btnGetTransactions
            // 
            this.btnGetTransactions.Location = new System.Drawing.Point(16, 233);
            this.btnGetTransactions.Margin = new System.Windows.Forms.Padding(4);
            this.btnGetTransactions.Name = "btnGetTransactions";
            this.btnGetTransactions.Size = new System.Drawing.Size(193, 28);
            this.btnGetTransactions.TabIndex = 9;
            this.btnGetTransactions.Text = "Get Transactions";
            this.btnGetTransactions.UseVisualStyleBackColor = true;
            this.btnGetTransactions.Click += new System.EventHandler(this.btnGetTransactions_Click);
            // 
            // btnGetTrendbars
            // 
            this.btnGetTrendbars.Location = new System.Drawing.Point(16, 268);
            this.btnGetTrendbars.Margin = new System.Windows.Forms.Padding(4);
            this.btnGetTrendbars.Name = "btnGetTrendbars";
            this.btnGetTrendbars.Size = new System.Drawing.Size(193, 28);
            this.btnGetTrendbars.TabIndex = 10;
            this.btnGetTrendbars.Text = "Get Trendbars";
            this.btnGetTrendbars.UseVisualStyleBackColor = true;
            this.btnGetTrendbars.Click += new System.EventHandler(this.btnGetTrendbars_Click);
            // 
            // btnGetTickData
            // 
            this.btnGetTickData.Location = new System.Drawing.Point(16, 304);
            this.btnGetTickData.Margin = new System.Windows.Forms.Padding(4);
            this.btnGetTickData.Name = "btnGetTickData";
            this.btnGetTickData.Size = new System.Drawing.Size(193, 28);
            this.btnGetTickData.TabIndex = 11;
            this.btnGetTickData.Text = "Get Tick Data";
            this.btnGetTickData.UseVisualStyleBackColor = true;
            this.btnGetTickData.Click += new System.EventHandler(this.btnGetTickData_Click);
            // 
            // btnGetSymbols
            // 
            this.btnGetSymbols.Location = new System.Drawing.Point(16, 126);
            this.btnGetSymbols.Margin = new System.Windows.Forms.Padding(4);
            this.btnGetSymbols.Name = "btnGetSymbols";
            this.btnGetSymbols.Size = new System.Drawing.Size(193, 28);
            this.btnGetSymbols.TabIndex = 12;
            this.btnGetSymbols.Text = "Get Symbols";
            this.btnGetSymbols.UseVisualStyleBackColor = true;
            this.btnGetSymbols.Click += new System.EventHandler(this.btnGetSymbols_Click);
            // 
            // btnSendStopOrder
            // 
            this.btnSendStopOrder.Location = new System.Drawing.Point(220, 126);
            this.btnSendStopOrder.Margin = new System.Windows.Forms.Padding(4);
            this.btnSendStopOrder.Name = "btnSendStopOrder";
            this.btnSendStopOrder.Size = new System.Drawing.Size(212, 28);
            this.btnSendStopOrder.TabIndex = 14;
            this.btnSendStopOrder.Text = "Send Stop Order";
            this.btnSendStopOrder.UseVisualStyleBackColor = true;
            this.btnSendStopOrder.Click += new System.EventHandler(this.btnSendStopOrder_Click);
            // 
            // btnSendLimitOrder
            // 
            this.btnSendLimitOrder.Location = new System.Drawing.Point(220, 161);
            this.btnSendLimitOrder.Margin = new System.Windows.Forms.Padding(4);
            this.btnSendLimitOrder.Name = "btnSendLimitOrder";
            this.btnSendLimitOrder.Size = new System.Drawing.Size(212, 28);
            this.btnSendLimitOrder.TabIndex = 15;
            this.btnSendLimitOrder.Text = "Send Limit Order";
            this.btnSendLimitOrder.UseVisualStyleBackColor = true;
            this.btnSendLimitOrder.Click += new System.EventHandler(this.btnSendLimitOrder_Click);
            // 
            // btnSendStopLimitOrder
            // 
            this.btnSendStopLimitOrder.Location = new System.Drawing.Point(220, 197);
            this.btnSendStopLimitOrder.Margin = new System.Windows.Forms.Padding(4);
            this.btnSendStopLimitOrder.Name = "btnSendStopLimitOrder";
            this.btnSendStopLimitOrder.Size = new System.Drawing.Size(212, 28);
            this.btnSendStopLimitOrder.TabIndex = 16;
            this.btnSendStopLimitOrder.Text = "Send Stop Limit Order";
            this.btnSendStopLimitOrder.UseVisualStyleBackColor = true;
            this.btnSendStopLimitOrder.Click += new System.EventHandler(this.btnSendStopLimitOrder_Click);
            // 
            // btnClosePosition
            // 
            this.btnClosePosition.Location = new System.Drawing.Point(219, 233);
            this.btnClosePosition.Margin = new System.Windows.Forms.Padding(4);
            this.btnClosePosition.Name = "btnClosePosition";
            this.btnClosePosition.Size = new System.Drawing.Size(213, 28);
            this.btnClosePosition.TabIndex = 17;
            this.btnClosePosition.Text = "Close Position";
            this.btnClosePosition.UseVisualStyleBackColor = true;
            this.btnClosePosition.Click += new System.EventHandler(this.btnClosePosition_Click);
            // 
            // txtPositionID
            // 
            this.txtPositionID.Location = new System.Drawing.Point(529, 235);
            this.txtPositionID.Margin = new System.Windows.Forms.Padding(4);
            this.txtPositionID.Name = "txtPositionID";
            this.txtPositionID.Size = new System.Drawing.Size(132, 22);
            this.txtPositionID.TabIndex = 18;
            // 
            // txtVolume
            // 
            this.txtVolume.Location = new System.Drawing.Point(755, 235);
            this.txtVolume.Margin = new System.Windows.Forms.Padding(4);
            this.txtVolume.Name = "txtVolume";
            this.txtVolume.Size = new System.Drawing.Size(132, 22);
            this.txtVolume.TabIndex = 19;
            // 
            // lblID
            // 
            this.lblID.AutoSize = true;
            this.lblID.Location = new System.Drawing.Point(440, 242);
            this.lblID.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblID.Name = "lblID";
            this.lblID.Size = new System.Drawing.Size(79, 17);
            this.lblID.TabIndex = 20;
            this.lblID.Text = "Position ID:";
            // 
            // lblVolume
            // 
            this.lblVolume.AutoSize = true;
            this.lblVolume.Location = new System.Drawing.Point(671, 240);
            this.lblVolume.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblVolume.Name = "lblVolume";
            this.lblVolume.Size = new System.Drawing.Size(59, 17);
            this.lblVolume.TabIndex = 21;
            this.lblVolume.Text = "Volume:";
            // 
            // btnClear
            // 
            this.btnClear.Location = new System.Drawing.Point(16, 416);
            this.btnClear.Margin = new System.Windows.Forms.Padding(4);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(193, 28);
            this.btnClear.TabIndex = 22;
            this.btnClear.Text = "Clear";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(4);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.refreshTokenButton);
            this.splitContainer1.Panel1.Controls.Add(this.lblAccounts);
            this.splitContainer1.Panel1.Controls.Add(this.cbAccounts);
            this.splitContainer1.Panel1.Controls.Add(this.lblTakeProfit);
            this.splitContainer1.Panel1.Controls.Add(this.txtTakeProfit);
            this.splitContainer1.Panel1.Controls.Add(this.lblStolLoss);
            this.splitContainer1.Panel1.Controls.Add(this.lblID2);
            this.splitContainer1.Panel1.Controls.Add(this.txtStopLoss);
            this.splitContainer1.Panel1.Controls.Add(this.txtPositionIDTPSL);
            this.splitContainer1.Panel1.Controls.Add(this.btnAmentSLTP);
            this.splitContainer1.Panel1.Controls.Add(this.btnAuthorizeApplication);
            this.splitContainer1.Panel1.Controls.Add(this.btnClear);
            this.splitContainer1.Panel1.Controls.Add(this.btnGetAccountsList);
            this.splitContainer1.Panel1.Controls.Add(this.lblVolume);
            this.splitContainer1.Panel1.Controls.Add(this.lblID);
            this.splitContainer1.Panel1.Controls.Add(this.btnSendMarketOrder);
            this.splitContainer1.Panel1.Controls.Add(this.txtVolume);
            this.splitContainer1.Panel1.Controls.Add(this.btnSubscribeForSpots);
            this.splitContainer1.Panel1.Controls.Add(this.txtPositionID);
            this.splitContainer1.Panel1.Controls.Add(this.btnUnSubscribeFrorSpots);
            this.splitContainer1.Panel1.Controls.Add(this.btnClosePosition);
            this.splitContainer1.Panel1.Controls.Add(this.btnGetDealsList);
            this.splitContainer1.Panel1.Controls.Add(this.btnSendStopLimitOrder);
            this.splitContainer1.Panel1.Controls.Add(this.btnGetOrders);
            this.splitContainer1.Panel1.Controls.Add(this.btnSendLimitOrder);
            this.splitContainer1.Panel1.Controls.Add(this.btnGetTransactions);
            this.splitContainer1.Panel1.Controls.Add(this.btnSendStopOrder);
            this.splitContainer1.Panel1.Controls.Add(this.btnGetTrendbars);
            this.splitContainer1.Panel1.Controls.Add(this.btnGetTickData);
            this.splitContainer1.Panel1.Controls.Add(this.btnGetSymbols);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.txtMessages);
            this.splitContainer1.Size = new System.Drawing.Size(1581, 708);
            this.splitContainer1.SplitterDistance = 364;
            this.splitContainer1.SplitterWidth = 5;
            this.splitContainer1.TabIndex = 23;
            // 
            // lblAccounts
            // 
            this.lblAccounts.AutoSize = true;
            this.lblAccounts.Location = new System.Drawing.Point(16, 96);
            this.lblAccounts.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblAccounts.Name = "lblAccounts";
            this.lblAccounts.Size = new System.Drawing.Size(63, 17);
            this.lblAccounts.TabIndex = 31;
            this.lblAccounts.Text = "Account:";
            // 
            // cbAccounts
            // 
            this.cbAccounts.FormattingEnabled = true;
            this.cbAccounts.Location = new System.Drawing.Point(97, 90);
            this.cbAccounts.Margin = new System.Windows.Forms.Padding(4);
            this.cbAccounts.Name = "cbAccounts";
            this.cbAccounts.Size = new System.Drawing.Size(112, 24);
            this.cbAccounts.TabIndex = 30;
            this.cbAccounts.SelectedIndexChanged += new System.EventHandler(this.cbAccounts_SelectedIndexChanged);
            // 
            // lblTakeProfit
            // 
            this.lblTakeProfit.AutoSize = true;
            this.lblTakeProfit.Location = new System.Drawing.Point(907, 274);
            this.lblTakeProfit.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblTakeProfit.Name = "lblTakeProfit";
            this.lblTakeProfit.Size = new System.Drawing.Size(81, 17);
            this.lblTakeProfit.TabIndex = 29;
            this.lblTakeProfit.Text = "Take Profit:";
            // 
            // txtTakeProfit
            // 
            this.txtTakeProfit.Location = new System.Drawing.Point(991, 271);
            this.txtTakeProfit.Margin = new System.Windows.Forms.Padding(4);
            this.txtTakeProfit.Name = "txtTakeProfit";
            this.txtTakeProfit.Size = new System.Drawing.Size(132, 22);
            this.txtTakeProfit.TabIndex = 28;
            // 
            // lblStolLoss
            // 
            this.lblStolLoss.AutoSize = true;
            this.lblStolLoss.Location = new System.Drawing.Point(671, 272);
            this.lblStolLoss.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStolLoss.Name = "lblStolLoss";
            this.lblStolLoss.Size = new System.Drawing.Size(75, 17);
            this.lblStolLoss.TabIndex = 27;
            this.lblStolLoss.Text = "Stop Loss:";
            // 
            // lblID2
            // 
            this.lblID2.AutoSize = true;
            this.lblID2.Location = new System.Drawing.Point(440, 274);
            this.lblID2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblID2.Name = "lblID2";
            this.lblID2.Size = new System.Drawing.Size(79, 17);
            this.lblID2.TabIndex = 26;
            this.lblID2.Text = "Position ID:";
            // 
            // txtStopLoss
            // 
            this.txtStopLoss.Location = new System.Drawing.Point(755, 268);
            this.txtStopLoss.Margin = new System.Windows.Forms.Padding(4);
            this.txtStopLoss.Name = "txtStopLoss";
            this.txtStopLoss.Size = new System.Drawing.Size(132, 22);
            this.txtStopLoss.TabIndex = 25;
            // 
            // txtPositionIDTPSL
            // 
            this.txtPositionIDTPSL.Location = new System.Drawing.Point(529, 267);
            this.txtPositionIDTPSL.Margin = new System.Windows.Forms.Padding(4);
            this.txtPositionIDTPSL.Name = "txtPositionIDTPSL";
            this.txtPositionIDTPSL.Size = new System.Drawing.Size(132, 22);
            this.txtPositionIDTPSL.TabIndex = 24;
            // 
            // btnAmentSLTP
            // 
            this.btnAmentSLTP.Location = new System.Drawing.Point(219, 268);
            this.btnAmentSLTP.Margin = new System.Windows.Forms.Padding(4);
            this.btnAmentSLTP.Name = "btnAmentSLTP";
            this.btnAmentSLTP.Size = new System.Drawing.Size(213, 28);
            this.btnAmentSLTP.TabIndex = 23;
            this.btnAmentSLTP.Text = "Amend Stop Loss/Take Profit";
            this.btnAmentSLTP.UseVisualStyleBackColor = true;
            this.btnAmentSLTP.Click += new System.EventHandler(this.btnAmentSLTP_Click);

            // 
            // refreshTokenButton
            // 
            this.refreshTokenButton.Location = new System.Drawing.Point(443, 18);
            this.refreshTokenButton.Margin = new System.Windows.Forms.Padding(4);
            this.refreshTokenButton.Name = "refreshTokenButton";
            this.refreshTokenButton.Size = new System.Drawing.Size(212, 28);
            this.refreshTokenButton.TabIndex = 32;
            this.refreshTokenButton.Text = "Refresh Token";
            this.refreshTokenButton.UseVisualStyleBackColor = true;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1581, 708);
            this.Controls.Add(this.splitContainer1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Main";
            this.ShowIcon = false;
            this.Text = "Open API Client";
            this.Load += new System.EventHandler(this.Main_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox txtMessages;
        private System.Windows.Forms.Button btnAuthorizeApplication;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Button btnGetAccountsList;
        private System.Windows.Forms.Button btnSendMarketOrder;
        private System.Windows.Forms.Button btnSubscribeForSpots;
        private System.Windows.Forms.Button btnUnSubscribeFrorSpots;
        private System.Windows.Forms.Button btnGetDealsList;
        private System.Windows.Forms.Button btnGetOrders;
        private System.Windows.Forms.Button btnGetTransactions;
        private System.Windows.Forms.Button btnGetTrendbars;
        private System.Windows.Forms.Button btnGetTickData;
        private System.Windows.Forms.Button btnGetSymbols;
        private System.Windows.Forms.Button btnSendStopOrder;
        private System.Windows.Forms.Button btnSendLimitOrder;
        private System.Windows.Forms.Button btnSendStopLimitOrder;
        private System.Windows.Forms.Button btnClosePosition;
        private System.Windows.Forms.TextBox txtPositionID;
        private System.Windows.Forms.TextBox txtVolume;
        private System.Windows.Forms.Label lblID;
        private System.Windows.Forms.Label lblVolume;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Label lblTakeProfit;
        private System.Windows.Forms.TextBox txtTakeProfit;
        private System.Windows.Forms.Label lblStolLoss;
        private System.Windows.Forms.Label lblID2;
        private System.Windows.Forms.TextBox txtStopLoss;
        private System.Windows.Forms.TextBox txtPositionIDTPSL;
        private System.Windows.Forms.Button btnAmentSLTP;
        private System.Windows.Forms.Label lblAccounts;
        private System.Windows.Forms.ComboBox cbAccounts;
        private System.Windows.Forms.Button refreshTokenButton;
    }
}

