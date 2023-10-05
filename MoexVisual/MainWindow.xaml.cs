
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;

using FinAlgoTrader.Fast;
using FinAlgoTrader.Fix.connector;
using FinAlgoTrader.Fix.data;
using FinAlgoTrader.Fix.helpers;

namespace MoexVisual
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        private FixConnector _fixConnector;
        private MoexFastClient _fastconnector;

        private string InstrumentA = "USDCNY_TOD"; //боевой - "USDCNY_TOM"
        private string InstrumentB = "USD000UTSTOM";
        private string InstrumentC = "CNYRUB_TOM";

        public bool FixConnectedAlready { get; set; }

        public MainWindow()
        {
            InitializeComponent();
         

            //настройки / счет / код клиента
            _fixConnector = new FixConnector("settings.ini", "L00+4190B503", "5056542474");

            _fixConnector.NewLog += LogMessage;

            _fixConnector.OnFixEvent += fixmessage => LogMessage("Fix-> " + fixmessage);

            _fixConnector.OnConnected += () =>
                {
                    //если фикс слетит - то будет повтор подключения
                    if (FixConnectedAlready)
                        return;

                    FixConnectedAlready = true;

                    StartFast();

                };

            _fixConnector.OnTextMessage += message => LogMessage("FixConnector-> " + message);


            _fixConnector.OnOrderCanceled += order => LogMessage($"Ордер Отменен {order.Symbol} {order.Side} {order.Price} {(order.Text)} {order.ClOrdID}");

            //_fixConnector.OnOrderNew += order => LogMessage($"Норвый ордер  {order.order.Symbol} {order.order.Side} {order.order.Price} {GetMessageNormal(order.order.Text)} {order.order.ClOrdID}");

            _fixConnector.OnOrderRejected += order =>
            {
                LogMessage($"НЕ принят! {order.order.Symbol} {order.order.Side} {order.order.Price} {(order.error)}");
            };

            //_fixConnector.OnOrderReplaced += order => LogMessage($"Ордер переставлен. Новый ордер -> {order.newOrder.Symbol} {order.newOrder.Side} {order.newOrder.Price} {GetMessageNormal(order.newOrder.Text)}  {order.newOrder.ClOrdID}");

            _fixConnector.OnOrderSuspended += order => LogMessage($"Ордер остранен {order.Symbol} {order.Side} {order.Price} {(order.Text)}  {order.ClOrdID}");

            _fixConnector.OnOrderCancelReplaceReject += order => LogMessage($"Отказ перестановки/отмены ордера {order.ClOrdID} {(order.Text)}");

            _fixConnector.OnNewTrade += trade => LogMessage($"Новая сделка {trade.trade.Symbol} {trade.trade.Side} обьем = {trade.trade.Qty}");

            _fixConnector.Start();

            LogMessage("старт");
        }

        private void StartFast()
        {
        
            LogMessage("Подключились к FIX, Включаем FAST");

            _fastconnector = new MoexFastClient();
            _fastconnector.NewLog += LogMessage;

            _fastconnector.Connect();

            SubscibeForOrderBook();
           
        }


        protected override void OnClosing(CancelEventArgs e)
        {
            if(_fastconnector!=null)
            _fastconnector.Disconnect();
            _fixConnector.Stop();
            
            base.OnClosing(e);
        }

        private void SubscibeForOrderBook()
        {
          
            _fastconnector.OrderBookIncrement.NewLog += (message) =>
            {
                LogMessage(message);
            };

            _fastconnector.OrderBookIncrement.QuoteUpdate += (quote) =>
            {
                if (Application.Current != null)
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {

                    if (InstrumentA == quote.Symbol)
                    {
                        //котировки основного инструмента 
                        ABestBidLabel.Content = quote.BestBid;
                        ABestAskLabel.Content = quote.BestAsk;
                    }
                    else if(InstrumentB == quote.Symbol)
                    {

                        BBestBidLabel.Content = quote.BestBid;
                        BBestAskLabel.Content = quote.BestAsk;
                        // котировки второго 

                    }
                    else if (InstrumentC == quote.Symbol)
                    {
                        CBestBidLabel.Content = quote.BestBid;
                        CBestAskLabel.Content = quote.BestAsk;

                        // котировки третьего 
                    }

                }));
            };
        }

        private void StartQuotes_Click_1(object sender, RoutedEventArgs e)
        {
            var Instuments = new List<string>() { InstrumentA, InstrumentB, InstrumentC};
            _fastconnector.AddInstrumentsToListen(Instuments);
        }

        private void StartFast_Click(object sender, RoutedEventArgs e)
        {
            StartFast();
        }

        private void SendOrderButton_Click(object sender, RoutedEventArgs e)
        {
            
            var clientid = _fixConnector.GenerateNextOrderId();

            var instrument = new FixInstrument()
            {
                NumbersAfterComma = 4,
                SecCode = "CETS",
                Symbol = "USDCNY_TOM" 
            };

            _fixConnector.PlaceOrder(instrument,
                7.30m,
                1,
               ESides.Buy,
                clientid);

            var newreplaceid = _fixConnector.GenerateNextOrderId();

            Thread.Sleep(10);

            _fixConnector.ReplaceOrder(instrument, 7.20m, 2, clientid, ESides.Buy, newreplaceid);

            Thread.Sleep(10);

            Debug.WriteLine("Ордер отмена" + newreplaceid);

            _fixConnector.Cancel(newreplaceid);


        }

        public async void LogMessage(string message)
        {

            try
            {

                var dt = DateTime.Now;
                var datetime = dt.ToString("H:mm:ss.fff");
                var logmessage = datetime + " | " + message;

                if (Application.Current != null)
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        LogTextBox.AppendText(logmessage + Environment.NewLine);
                        LogTextBox.ScrollToEnd();

                    }));
            }
            catch (Exception ex)
            { Debug.WriteLine(ex.Message); }

        }
    }
}
