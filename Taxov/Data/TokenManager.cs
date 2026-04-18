using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using DataLibrary;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Taxov.Data
{
    public class TokenManager
    {      
        [CascadingParameter] public AuthenticationState authState { get; set; }
        public AuthenticationStateProvider _authenticationStateProvider;

        public IDataAccess _data;
        public IConfiguration _config;

        Task _workerLoop;
        CancellationTokenSource _cts;

        protected string auth0ClientId;
        protected string auth0Secret;
        protected AuthenticationApiClient authenticationApiClient;

        //Token from API

        public AccessTokenResponse MToken;

        public string _MgmtToken;
        public string MgmtToken { get { return _MgmtToken; } }

        public long _timeBeforeTokenExpires;
        DateTimeOffset _tokenExpiresAt;

        //CTOR
        public TokenManager(IConfiguration config)
        {
            Console.WriteLine("Token Manager Initialized - CTOR");
            _config = config;            
            Reset();
            Initialize();
        }

        //Reset to Start
        private void Reset()
        {
            Console.WriteLine("Resetting Token Manager to default settings.");
            if (_cts != null)
            {
                _cts.Dispose();
                _cts = null;
            }
            if (_workerLoop != null)
            {
                _workerLoop = null;
            }

            MToken = null;
            _MgmtToken = null;
            _timeBeforeTokenExpires = -1;
            _tokenExpiresAt = DateTimeOffset.MinValue;
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
            auth0ClientId = _config.GetValue<string>("Auth0:ClientId");
            auth0Secret = _config.GetValue<string>("Auth0:ClientSecret");
            authenticationApiClient = new AuthenticationApiClient("dev-xu94ajki.us.auth0.com");

            try
            {
                var mtoken = await GetMgmtTokenFromAPI();

                if(mtoken != null)
                {
                    SaveToken(mtoken);
                }
                else
                {
                    Reset();
                }
                

                _cts = new CancellationTokenSource();
                _workerLoop = new Task(RequestNewToken);
                _workerLoop.Start();

            }
            catch(Exception e)
            {
                Console.WriteLine("Error on Initializion - Token Manager - " + e);
                Reset();
                return false;
            }
            return true;
        }    

        //Get Token from Auth0 MGMT API
        public async Task<AccessTokenResponse> GetMgmtTokenFromAPI()
        {
            //Console.WriteLine("Getting token from Auth0 API");

            AccessTokenResponse token = null;

            try
            {
                // Get Access Token
                token = await authenticationApiClient.GetTokenAsync(new ClientCredentialsTokenRequest
                {
                    ClientId = auth0ClientId,
                    ClientSecret = auth0Secret,
                    Audience = "https://dev-xu94ajki.us.auth0.com/api/v2/"
                });
            }
            catch(Exception e)
            {
                Console.WriteLine("Attemping to get token from Auth0. " + e);
                return null;
            }
            return token;
        }

        //Save Token
        private void SaveToken(AccessTokenResponse token)
        {
            Console.WriteLine("Saving token");

            _MgmtToken = token.AccessToken;
            _timeBeforeTokenExpires = token.ExpiresIn;
            _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(_timeBeforeTokenExpires);

            Console.WriteLine("Has Access Token: {0}", !string.IsNullOrWhiteSpace(_MgmtToken));
            Console.WriteLine("Token expires in {0}s", _timeBeforeTokenExpires);
            Console.WriteLine("Token expires at {0}", _tokenExpiresAt);
        }

        async void RequestNewToken()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                //Console.WriteLine("Worker loop started.");

                var delayBy = (_timeBeforeTokenExpires * 0.90M) * 1000;

                Console.WriteLine("Waiting {0} seconds before refreshing Mgmt token", delayBy / 1000);

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

                AccessTokenResponse token = null;
                do
                {
                    try
                    {
                        token = await GetMgmtTokenFromAPI();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Received an error while renewing the token... " + e);
                    }

                    if (token == null)
                    {
                        Console.WriteLine("Could not renew the token.  Waiting 10 seconds, and trying again.");
                        await Task.Delay(TimeSpan.FromSeconds(10));
                    }
                }
                while (token == null && _tokenExpiresAt < DateTimeOffset.UtcNow);

                if (token == null)
                {
                    Reset();
                    return;
                }

                SaveToken(token);
            }
        }

    }
}
