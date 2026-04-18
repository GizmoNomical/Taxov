using DataLibrary;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Taxov.Data
{
    public class ServerStatusManager
    {     
        public IDataAccess _data;
        public IConfiguration _config;

        Task _workerLoop;
        CancellationTokenSource _cts;        

        public long _timeBeforePollingAgain = 5;
        DateTimeOffset _pollTimeExpiresAt;

        public static bool ServerOnline = true;
        public static bool ServerChangedState = false;
        public static string ServerPreviousState = "Online";

        private bool status;

        //RSS FEED PARAMS
        int RSSUpdateCounter = 0;

        private string RSSFeedPath = "/wwwroot/RSSDisplay/RSSFeed.txt";

        private string[] RSSData = Array.Empty<string>();
        private int RSSDataLength = 0;
        private int currentElement = 0;

        public string RSSElementA = string.Empty;
        public string RSSElementB = "Welcome to Taxov!";




        //CTOR
        public ServerStatusManager(IConfiguration config)
        {
            Console.WriteLine("Server Manager Initialized - CTOR");
            _config = config;
            Reset();
            Initialize();
        }

        //Reset to Start
        private void Reset()
        {
            Console.WriteLine("Resetting Server Status Manager to default settings.");
            if (_cts != null)
            {
                _cts.Dispose();
                _cts = null;
            }
            if (_workerLoop != null)
            {
                _workerLoop = null;
            }

            _pollTimeExpiresAt = DateTimeOffset.MinValue;
        }

        //Stop WorkerLoop
        public Task Stop()
        {
            _cts.Cancel();
            Reset();
            return _workerLoop;
        }

        //Constructor - Setup Variables as Singleton
        public async Task<Boolean> Initialize()
        {           
            try
            {
                await GetTickerData();
                //await UpdateTicker();
                _cts = new CancellationTokenSource();
                _workerLoop = new Task(RequestServerStatus);
                _workerLoop.Start();

            }
            catch (Exception e)
            {
                Console.WriteLine("Error on Initialization - Server Status Manager - " + e);
                Reset();
                return false;
            }
            return true;
        }

        //Save Token
        private async void SaveServerStatus(bool Status)
        {
            //ServerOnline = Status;

            //Force Online Bypass
            ServerOnline = true;


            //Server State Change Checks
            if (ServerOnline)
            {
                if(ServerPreviousState == "Online")
                {
                    ServerChangedState = false;
                }
                if(ServerPreviousState == "Offline")
                {
                    ServerChangedState = true;
                }
            }
            if (!ServerOnline)
            {
                if (ServerPreviousState == "Offline")
                {
                    ServerChangedState = false;
                }
                if (ServerPreviousState == "Online")
                {
                    ServerChangedState = true;
                }                
            }

            await UpdateTicker();
            OnServerPolled(null);

            //Console.WriteLine("Saving Server Status");
            if(ServerOnline)
            {
                Console.WriteLine("Server Status: Online");
                //Console.WriteLine("Server Previous State: " + ServerPreviousState);
                //Console.WriteLine("Server Changed State? " + ServerChangedState);

                ServerPreviousState = "Online";
            }
            if(!ServerOnline)
            {
                Console.WriteLine("Server Status: Offline");
                //Console.WriteLine("Server Previous State: " + ServerPreviousState);
                //Console.WriteLine("Server Changed State? " + ServerChangedState);

                ServerPreviousState = "Offline";
            }
            
            _pollTimeExpiresAt = DateTimeOffset.UtcNow.AddSeconds(_timeBeforePollingAgain);

            
        }

        //Declare Update Event
        public event EventHandler ServerPolled;

        protected virtual void OnServerPolled(EventArgs e)
        {
            EventHandler handler = ServerPolled;
            handler?.Invoke(this, e);
        }


        private async Task<Boolean> PollServer()
        {
            //Console.WriteLine("Polling Database for Server Status...");

            //if (await DataAccess.CheckConnection(_config.GetConnectionString("pollDB")))
            //{
            //    _pollTimeExpiresAt = DateTimeOffset.UtcNow.AddSeconds(_timeBeforePollingAgain);
            //    return true;
            //}
            //else
            //{
            //    _pollTimeExpiresAt = DateTimeOffset.UtcNow.AddSeconds(_timeBeforePollingAgain);
            //    return false;
            //}

            //BYPASS
            _pollTimeExpiresAt = DateTimeOffset.UtcNow.AddSeconds(_timeBeforePollingAgain);
            return true;
        }

        async void RequestServerStatus()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                //Console.WriteLine("Worker loop started.");

                var delayBy = (_timeBeforePollingAgain) * 1000;

                Console.WriteLine("Waiting {0} seconds before polling Database Server", delayBy / 1000);
                Console.WriteLine(" ");

                try
                {
                    await Task.Delay(Convert.ToInt32(delayBy), _cts.Token);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Task...delay failed? " + e);
                }
                if (_cts.Token.IsCancellationRequested)
                {
                    Console.WriteLine("Cancellation was requested... ending worker");
                    return;
                }

                do
                {
                    try
                    {
                        status = await PollServer();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Received an error while polling the server... " + e);
                    }
                }
                while (_pollTimeExpiresAt < DateTimeOffset.UtcNow);                

                SaveServerStatus(status);
            }
        }

        public async Task GetTickerData()
        {
            if (ServerOnline)
            {
                RSSFeedPath = "/wwwroot/RSSDisplay/RSSFeed.txt";
            }

            if (!ServerOnline)
            {
                RSSFeedPath = "/wwwroot/RSSDisplay/RSSError.txt";
            }

            RSSData = Array.Empty<string>();

            try
            {
                RSSData = System.IO.File.ReadAllLines($"{System.IO.Directory.GetCurrentDirectory()}{RSSFeedPath}");
                RSSDataLength = RSSData.Length;
                currentElement = 0;
            }
            catch
            {
                RSSData = Array.Empty<string>();
                RSSDataLength = 0;
            }
        }


        //RSS FEED

        public async Task UpdateTicker()
        {
            if(ServerChangedState)
            {
                await GetTickerData();
            }

            string RSSCurrentText = string.Empty;

            try
            {
                if (currentElement >= RSSDataLength)
                {
                    currentElement = 0;
                }

                try
                {
                    RSSCurrentText = RSSData[currentElement];
                }
                catch
                {
                    RSSCurrentText = RSSData[0];
                }

                RSSElementA = string.Empty;
                RSSElementA = RSSElementB;
                RSSElementB = string.Empty;
                RSSElementB = RSSCurrentText;

                RSSUpdateCounter++;
                currentElement++;

                if(ServerOnline)
                {
                    if (RSSUpdateCounter > 59)
                    {
                        RSSUpdateCounter = 0;
                        RSSElementA = string.Empty;
                        RSSElementB = "Loading RSS Feed Data...";
                        await GetTickerData();
                    }
                }

                if (!ServerOnline)
                {
                    if (RSSUpdateCounter > 11)
                    {
                        RSSUpdateCounter = 0;
                        RSSElementA = string.Empty;
                        RSSElementB = "Loading RSS Feed Data...";
                        await GetTickerData();
                    }
                }

            }
            catch
            {
                RSSElementA = string.Empty;
                RSSElementB = string.Empty;
            }
        }


    }
}
