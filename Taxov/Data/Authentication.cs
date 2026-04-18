using Microsoft.AspNetCore.Components;
using Stripe;
using Stripe.Checkout;
using Taxov.Pages;
using System;
using System.Configuration;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Components.Authorization;
using Auth0.ManagementApi.Models;
using System.Data;
using Taxov.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;
using Auth0.ManagementApi;
using DataLibrary;
using Microsoft.Extensions.Configuration;
using RestSharp;
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Diagnostics;

namespace Taxov.Data;
public class Authentication
{
    #region Base Authentication

    public readonly AuthenticationStateProvider _authenticationStateProvider;
    public IDataAccess _data;
    public IConfiguration _config;
    public TokenManager _tokenManager;
    private readonly HttpClient _http;

    public UserModel CurrentUser;
    public List<UserModel> user;
    public List<TokenModel> mtoken;
    public int TokenTimeOut = 14400;

    public string userId = "";
    public string mgmtClientToken;
    public string GUIDHash;

    public User Auth0Client = null;

    public string AuthenticatedUser;

    public bool DatabaseOnline;

    private int retryCount = 3;
    private readonly TimeSpan retryDelay = TimeSpan.FromSeconds(3);



    [CascadingParameter] public AuthenticationState authState { get; set; }

    public Authentication(AuthenticationStateProvider authenticationStateProvider, IDataAccess data, IConfiguration config, TokenManager tokenManager, HttpClient http)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _data = data;
        _config = config;
        _tokenManager = tokenManager;
        _http = http;
        StripeConfiguration.ApiKey = _config.GetValue<string>("Stripe:ApiKey");
        GUIDHash = _config.GetValue<string>("Stripe:GUID");

        try
        {
            authState = _authenticationStateProvider.GetAuthenticationStateAsync().Result;
        }
        catch
        {            
            authState = null;
            return;
        }

        UpdateDatabaseStatus();
        
        Auth0Client = new User();

        if (_tokenManager.MgmtToken == null)
        {
            return;
        }
        

        if (authState.User.Identity.IsAuthenticated && _tokenManager.MgmtToken != null && ServerStatusManager.ServerOnline)
        {
            Console.WriteLine("Setup User from Authentication - CTOR");
            _ = SetupActiveUserRetryWhenFail();
        }
        else
        {
            Console.WriteLine("User Not Authenticated = Authentication CTOR");
            return;
        }
    }

    private bool IsTransient(Exception e)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateDatabaseStatus()
    {
        try
        {
            if (await DataAccess.CheckConnection(_config.GetConnectionString("pollDB")))
            {
                //Console.WriteLine("Database is loaded and active");
                DatabaseOnline = true;
            }
            else
            {
                Console.WriteLine("Database is not loaded!  Maintenance?  System down?");
                DatabaseOnline = false;
                return;
            }
        }
        catch
        {
            Console.WriteLine("Database has crashed!  Shit is fuckin' Up!");
            DatabaseOnline = false;
            return;
        }
    }


    public async Task<Boolean> UserSetup()
    {
        if(!ServerStatusManager.ServerOnline)
        {
            return false;
        }

        UpdateDatabaseStatus();

        if (!DatabaseOnline)
        {
            return false;
        }

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client null at UserSetup");
            return false;
        }
        else
        {
            try
            {
                await InsertNewUser();
                Console.WriteLine("New User Inserted: " + Auth0Client.UserName);
                return true;
            }
            catch
            {
                Console.WriteLine("Insert New User Failed from UserSetup.");
                return false;
            }
        }
    }
    public async Task<Boolean> SetupActiveUserRetryWhenFail()
    {
        int currentRetry = 0;

        for (; ; )
        {
            try
            {
                if (await SetupActiveUser())
                {
                    return true;
                }
                else
                {
                    Console.WriteLine("SetupActiveUserRetryWhenFail Operation Failed");

                    currentRetry++;

                    if (currentRetry > this.retryCount)
                    {
                        Console.WriteLine("Setup Active User Failed Consecutively...");
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("SetupActiveUserRetryWhenFail Operation Failed");

                currentRetry++;

                if (currentRetry > this.retryCount || !IsTransient(e))
                {
                    Console.WriteLine("Setup Active User Failed Consecutively...");
                    return false;
                }
            }
            await Task.Delay(retryDelay);
        }
        return true;
    }
    public async Task<Boolean> SetupActiveUser()
    {
        if(!ServerStatusManager.ServerOnline)
        {
            return false;
        }

        UpdateDatabaseStatus();

        if (!DatabaseOnline)
        {
            return false;
        }

        if (CreateUserProfileData())
        {
            if (await GetAuth0UserID())
            {
                if (_tokenManager.MgmtToken != null)
                {
                    await GetAuth0DataRetryWhenFail();
                   
                    if (await CheckUserInDatabase())
                    {
                        //await PopulateUserInfoFromDB();
                        await GetUserProfileDataFromDB();
                        return true;
                    }
                    else
                    {
                        if (await UserSetup())
                        {
                            //await PopulateUserInfoFromDB();
                            await GetUserProfileDataFromDB();
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
        }
        else
        {
            Console.WriteLine("Could Not Create UserProfileData");
            return false;
        }
        return true;
    }
    public bool CreateUserProfileData()
    {
        if (CurrentUser == null)
        {
            try
            {
                CurrentUser = new UserModel();
                //Populate USerProfile from DB
                return true;
            }
            catch
            {
                return false;
            }
        }
        return true;
    }
    public async Task PopulateUserInfoFromDB()
    {
        if (!ServerStatusManager.ServerOnline)
        {
            return;
        }

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client is Null.  Cannot Populate Info from DB");
            return;
        }
        else
        {
            CurrentUser.UserName = Auth0Client.UserName;
            await GetClientIDFromDB();
            await GetUserEmailFromDB();
            await GetPaidStatusFromDB();
            await GetSubTierFromDB();
            await CheckSubTier();
            await CheckAccessLevel();
            await GetAccessLevelFromDB();
            await GetSubTierNameFromDB();
            //await RefreshMgmtToken();
        }
    }
    public async Task<Boolean> GetLoggedInUser()
    {
        if (authState == null)
        {
            Console.WriteLine("authState Null - Cannot Get Logged in User");
            return false;
        }
        else
        {
            authState = _authenticationStateProvider.GetAuthenticationStateAsync().Result;
            return true;
            //AuthenticatedUser = authState.User.Identity.Name;
        }
    }
    public async Task<Boolean> GetAuth0UserID()
    {
        if (authState == null)
        {
            Console.WriteLine("authState Null - Cannot Get Auth0 UserId");
            return false;
        }
        
        try
        {
            userId = authState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value;
            return true;
        }
        catch
        {
            Console.WriteLine("Could not find valid Auth0 UserID");
            return false;
        }        
    }
    public async Task<Boolean> GetAuth0DataRetryWhenFail()
    {
        int currentRetry = 0;

        for (; ; )
        {
            try
            {
                if(await GetAuth0Data())
                {
                    return true;
                }
                else
                {
                    Console.WriteLine("Get Auth0Data Operation Failed");

                    currentRetry++;

                    if (currentRetry > this.retryCount)
                    {
                        Console.WriteLine("Auth0Data Failed Consecutively...");
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Get Auth0Data Operation Failed");

                currentRetry++;

                if (currentRetry > this.retryCount || !IsTransient(e))
                {
                    Console.WriteLine("Auth0Data Failed Consecutively...");
                    return false;
                }
            }
            await Task.Delay(retryDelay);
        }
        return false;
    }
    public async Task<Boolean> GetAuth0Data()
    {
        if (userId == null || authState == null)
        {
            Console.WriteLine("authState is null or userID Not Found.");
            return false;
        }
        else
        {
            try
            {
                if (_tokenManager.MgmtToken == null)
                {
                    Console.WriteLine("Cannot get Auth0Data - Management Token is NULL");
                    return false;
                }

                var apiClient = new ManagementApiClient(_tokenManager.MgmtToken, new Uri("https://dev-xu94ajki.us.auth0.com/api/v2/"));
                try
                {
                    Auth0Client = await apiClient.Users.GetAsync(userId);
                    return true;
                }
                catch (Exception e)
                {
                    if( e.Message == "The user does not exist.")
                    {
                        Auth0Client = null;
                        return false;
                    }
                }
                return false;
            }
            catch
            {
                Console.WriteLine("Error getting Auth0 Data.");
                return false;
            }
        }
    }    
    public async Task<string> CheckAccessLevel()
    {
        if (!ServerStatusManager.ServerOnline)
        {
            return "Bolt Action";
        }

        //BOLT ACTION
        if (CurrentUser.SubTier == 0)
        {
            await UpdateAccessLevelInDB("Bolt Action");
            return "Bolt Action";
        }
        //SEMI-AUTO
        if (CurrentUser.SubTier == 18 && CurrentUser.PaidStatus == 0)
        {
            await UpdateAccessLevelInDB("Bolt Action");
            return "Bolt Action";
        }
        if (CurrentUser.SubTier == 18 && CurrentUser.PaidStatus == 1)
        {
            await UpdateAccessLevelInDB("Semi-Auto");
            return "Semi-Auto";
        }
        //FULL AUTO
        if (CurrentUser.SubTier == 88 && CurrentUser.PaidStatus == 0)
        {
            await UpdateAccessLevelInDB("Bolt Action");
            return "Bolt Action";
        }
        if (CurrentUser.SubTier == 88 && CurrentUser.PaidStatus == 1)
        {
            await UpdateAccessLevelInDB("Full Auto");
            return "Full Auto";
        }
        //BELT FED
        if (CurrentUser.SubTier == 150 && CurrentUser.PaidStatus == 0)
        {
            await UpdateAccessLevelInDB("Bolt Action");
            return "Bolt Action";
        }
        if (CurrentUser.SubTier == 150 && CurrentUser.PaidStatus == 1)
        {
            await UpdateAccessLevelInDB("Belt Fed");
            return "Belt Fed";
        }
        //INFLUENCER
        if (CurrentUser.SubTier == 234 && CurrentUser.LifetimeSub == 1)
        {
            await UpdateAccessLevelInDB("Influencer");
            return "Influencer";
        }
        if (CurrentUser.SubTier == 333 && CurrentUser.LifetimeSub == 1)
        {
            await UpdateAccessLevelInDB("GameMaster");
            return "GameMaster";
        }
        else
        {
            return "Bolt Action";
        }        
    }
    public async Task CheckSubTier()
    {
        if (!ServerStatusManager.ServerOnline)
        {
            return;
        }

        if (CurrentUser.SubTier == 0)
        {
            await UpdateSubTierNameInDB("Bolt Action");
        }
        if (CurrentUser.SubTier == 18)
        {
            await UpdateSubTierNameInDB("Semi-Auto");
        }
        if (CurrentUser.SubTier == 88)
        {
            await UpdateSubTierNameInDB("Full Auto");
        }
        if (CurrentUser.SubTier == 150)
        {
            await UpdateSubTierNameInDB("Belt Fed");
        }
        if (CurrentUser.SubTier == 234)
        {
            await UpdateSubTierNameInDB("Influencer");
        }
        if (CurrentUser.SubTier == 333)
        {
            await UpdateSubTierNameInDB("GameMaster");
        }
    }


    #endregion

    #region Database
    ////DATABASE////
    public async Task RefreshUserList()
    {
        if (!ServerStatusManager.ServerOnline)
        {
            user = null;
            return;
        }

        string sql = "select * from user";

        user = null;
        try
        {
            user = await _data.LoadData<UserModel, dynamic>(sql, new { }, _config.GetConnectionString("default"));
        }
        catch
        {
            user = null;
        }
        
    }
    
    //NEW USER
    public async Task InsertNewUser()
    {
        if(!ServerStatusManager.ServerOnline)
        {
            return;
        }

        UpdateDatabaseStatus();

        if (!DatabaseOnline)
        {
            return;
        }

        string sql = "insert into user (UserID, Email, UserName, CustomerID) values (@UserID, @Email, @UserName, @CustomerID)";
        string sql2 = "insert into usersettings (UserName) values (@UserName)";

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client Null at InsertNewUser");
            return;
        }
        else
        {
            try
            {
                //string _customer = await CreateStripeCustomer(Auth0Client.Email);
                await _data.SaveData(sql, new { UserID = Auth0Client.UserId, Email = Auth0Client.Email, UserName = Auth0Client.UserName, CustomerID = await CreateStripeCustomer(Auth0Client.Email) }, _config.GetConnectionString("default"));
                await _data.SaveData(sql2, new { UserName = Auth0Client.UserName }, _config.GetConnectionString("default"));
                return;
            }
            catch
            {
                Console.WriteLine("InsertNewUser Failed from DB Command.");
                return;
            }
        }
    }
    public async Task<Boolean> CheckUserInDatabase()
    {           
        await RefreshUserList();
        //await GetLoggedInUser();
        var userInDatabase = false;

        if(user == null)
        {
            return true;
        }

        foreach (var u in user)
        {
            if (u.Email.ToString() == Auth0Client.Email || u.UserName.ToString() == Auth0Client.UserName || u.UserID.ToString() == Auth0Client.UserId)
            {
                userInDatabase = true;
            }
        }

        if (userInDatabase)
        {
            Console.WriteLine("User already exists!.");
            return true;
        }
        else
        {
            Console.WriteLine("User does not exist.");
            return false;
        }
    }

    //GET FUNCTIONS

    //User Object
    public async Task<UserModel> GetUserProfileDataFromDB()
    {
        if (!ServerStatusManager.ServerOnline)
        {
            return null;
        }

        string sql = "select * from user where Username = @Username";
        
        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client Null at GetClientIDFromDB");
            return null;
        }
        else
        {
            try
            {
                if(CurrentUser == null)
                {
                    CreateUserProfileData();
                }

                user = await _data.LoadData<UserModel, dynamic>(sql, new { UserName = Auth0Client.UserName }, _config.GetConnectionString("checkDB"));

                if (user != null)
                {
                    CurrentUser.ClientID = user.ElementAt(0).ClientID;
                    CurrentUser.UserName = user.ElementAt(0).UserName;
                    CurrentUser.UserID = user.ElementAt(0).UserID;
                    CurrentUser.Email = user.ElementAt(0).Email;
                    CurrentUser.PaidStatus = user.ElementAt(0).PaidStatus;
                    CurrentUser.SubTier = user.ElementAt(0).SubTier;
                    CurrentUser.SubTierName = user.ElementAt(0).SubTierName;
                    //CurrentUser.AccessLevel = user.ElementAt(0).AccessLevel;
                    CurrentUser.CustomerID = user.ElementAt(0).CustomerID;
                    CurrentUser.SubscriptionID = user.ElementAt(0).SubscriptionID;
                    CurrentUser.SubTimeRemaining = user.ElementAt(0).SubTimeRemaining;
                    CurrentUser.LifetimeSub = user.ElementAt(0).LifetimeSub;
                    CurrentUser.NewCustomerDate = user.ElementAt(0).NewCustomerDate;
                    CurrentUser.AccessLevel = await CheckAccessLevel();

                    await GetUserSettingsFromDB();
                    Console.WriteLine("Retrieved User Data from DB");
                    return CurrentUser;
                }
            }
            catch
            {
                Console.WriteLine("Could not retreive Profile Data From Database");
                return null;
            }
        }
        //If Success
        return CurrentUser;


    }

    public async Task<UserModel> GetUserSettingsFromDB()
    {
        if (!ServerStatusManager.ServerOnline)
        {
            return null;
        }

        string sql = "select * from usersettings where Username = @Username";

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client Null at GetUserSettingsIDFromDB");
            return null;
        }
        else
        {
            try
            {
                user = await _data.LoadData<UserModel, dynamic>(sql, new { UserName = Auth0Client.UserName }, _config.GetConnectionString("default"));

                if (user != null)
                {
                    CurrentUser.ToolTips = user.ElementAt(0).ToolTips;
                    CurrentUser.Sound = user.ElementAt(0).Sound;
                    CurrentUser.Newsletter = user.ElementAt(0).Newsletter;
                    CurrentUser.LevelThreeIntel = user.ElementAt(0).LevelThreeIntel;

                    return CurrentUser;
                }
            }
            catch
            {
                Console.WriteLine("Could not retreive User Settings From Database");
                return null;
            }
        }
        //If Success
        return CurrentUser;


    }


    //Individual Data Access
    public async Task<int> GetClientIDFromDB()
    {
        if (!ServerStatusManager.ServerOnline)
        {
            return -1;
        }

        string sql = "select ClientID from user where Username = @Username";

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client Null at GetClientIDFromDB");
            return -1;
        }
        else
        {
            try
            {
                user = await _data.LoadData<UserModel, dynamic>(sql, new { UserName = Auth0Client.UserName }, _config.GetConnectionString("default"));

                if (user != null)
                {
                    //CurrentUserProfile.ClientID = user.ElementAt(0).ClientID;
                    return user.ElementAt(0).ClientID;
                }
            }
            catch
            {
                Console.WriteLine("Could not retreive ClientID From Database");
                return -1;
            }
        }
        //If Success
        return user.ElementAt(0).ClientID;
    }
    public async Task<string> GetUserEmailFromDB()
    {
        if (!ServerStatusManager.ServerOnline)
        {
            return null;
        }

        string sql = "select Email from user where UserName = @UserName";

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client Null at GetUserEmailFromDB");
            return null;
        }
        else
        {
            try
            {
                user = await _data.LoadData<UserModel, dynamic>(sql, new { UserName = Auth0Client.UserName }, _config.GetConnectionString("default"));

                if (user != null)
                {
                    CurrentUser.Email = user.ElementAt(0).Email;
                    return user.ElementAt(0).Email;
                }
            }
            catch
            {
                Console.WriteLine("Could not retreive Email from Database");
                return null;
            }
        }
        return user.ElementAt(0).Email;
    }
    public async Task<string> GetUserIDFromDB()
    {
        if (!ServerStatusManager.ServerOnline)
        {
            return null;
        }

        string sql = "select UserID from user where UserName = @UserName";

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client Null at GetUserIDFromDB");
            return null;
        }
        else
        {
            try
            {
                user = await _data.LoadData<UserModel, dynamic>(sql, new { UserName = Auth0Client.UserName }, _config.GetConnectionString("default"));
                if (user != null)
                {
                    //CurrentUserProfile.UserID = user.ElementAt(0).UserID;
                    return user.ElementAt(0).UserID;
                }
            }
            catch
            {
                Console.WriteLine("Could not retreive UserID from Database.");
                return null;
            }
        }
        return user.ElementAt(0).UserID;
    }
    public async Task<string> GetUserNameFromDB()
    {
        if (!ServerStatusManager.ServerOnline)
        {
            return null;
        }

        string sql = "select UserName from user where UserID = @UserID";

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client Null at GetUserNameFromDB");
            return null;
        }
        else
        {
            try
            {
                user = await _data.LoadData<UserModel, dynamic>(sql, new { UserID = Auth0Client.UserId }, _config.GetConnectionString("default"));
                if (user != null)
                {
                    //CurrentUserProfile.UserName = user.ElementAt(0).UserName;
                    return user.ElementAt(0).UserName;
                }
            }
            catch
            {
                Console.WriteLine("Could not retreive UserName from Database.");
                return null;
            }
        }
        return user.ElementAt(0).UserName;
    }
    public async Task<string> GetCustomerIDFromDB()
    {
        if (!ServerStatusManager.ServerOnline)
        {
            return null;
        }

        string sql = "select CustomerID from user where UserName = @UserName";

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client Null at GetCustomerIDFromDB");
            return null;
        }
        else
        {
            try
            {
                user = await _data.LoadData<UserModel, dynamic>(sql, new { UserName = Auth0Client.UserName }, _config.GetConnectionString("default"));
                if (user != null)
                {
                    //CurrentUserProfile.CustomerID = user.ElementAt(0).CustomerID;
                    return user.ElementAt(0).CustomerID;
                }
            }
            catch
            {
                Console.WriteLine("Could not retreive CustomerID From Database.");
                return null;
            }
        }
        return user.ElementAt(0).CustomerID;
    }
    public async Task<string> GetSubscriptionIDFromDB()
    {
        if (!ServerStatusManager.ServerOnline)
        {
            return null;
        }

        string sql = "select SubscriptionID from user where UserName = @UserName";

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client Null at GetSubscriptionIDFromDB");
            return null;
        }
        else
        {
            try
            {
                user = await _data.LoadData<UserModel, dynamic>(sql, new { UserName = Auth0Client.UserName }, _config.GetConnectionString("default"));
                if (user != null)
                {
                    CurrentUser.SubscriptionID = user.ElementAt(0).SubscriptionID;
                    if (user.ElementAt(0).SubscriptionID == null)
                    {
                        return null;
                    }
                    return user.ElementAt(0).SubscriptionID;
                }
            }
            catch
            {
                Console.WriteLine("Coult not retreive Subscription ID from Database");
                return null;
            }
        }
        return user.ElementAt(0).SubscriptionID;
    }
    public async Task<int> GetPaidStatusFromDB()
    {
        if (!ServerStatusManager.ServerOnline)
        {
            return 0;
        }

        string sql = "select PaidStatus from user where UserName = @UserName";

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client Null at GetPaidStatusFromDB");
            return -1;
        }
        else
        {
            try
            {
                user = await _data.LoadData<UserModel, dynamic>(sql, new { UserName = Auth0Client.UserName }, _config.GetConnectionString("default"));
                if (user != null)
                {
                    CurrentUser.PaidStatus = user.ElementAt(0).PaidStatus;
                    return user.ElementAt(0).PaidStatus;
                }
            }
            catch
            {
                Console.WriteLine("Could not retreive Paid Status from Database");
                return -1;
            }
        }
        return user.ElementAt(0).PaidStatus;
    }
    
    public async Task<int> GetSubTierFromDB()
    {
        if (!ServerStatusManager.ServerOnline)
        {
            return 0;
        }

        string sql = "select SubTier from user where UserName = @UserName";

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client Null at GetSubTierFromDB");
            return 0;
        }
        else
        {
            try
            {
                user = await _data.LoadData<UserModel, dynamic>(sql, new { UserName = Auth0Client.UserName }, _config.GetConnectionString("default"));
                if (user != null)
                {
                    CurrentUser.SubTier = user.ElementAt(0).SubTier;
                    return user.ElementAt(0).SubTier;
                }
            }
            catch
            {
                Console.WriteLine("Could not retreive Sub Tier from Database.");
                return 0;
            }
        }
        return user.ElementAt(0).SubTier;
    }
    public async Task<string> GetSubTierNameFromDB()
    {
        if (!ServerStatusManager.ServerOnline)
        {
            return null;
        }

        string sql = "select SubTierName from user where UserName = @UserName";

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client Null at GetSubTierNameFromDB");
            return null;
        }
        else
        {
            try
            {
                user = await _data.LoadData<UserModel, dynamic>(sql, new { UserName = Auth0Client.UserName }, _config.GetConnectionString("default"));
                if (user != null)
                {
                    CurrentUser.SubTierName = user.ElementAt(0).SubTierName;
                    return user.ElementAt(0).SubTierName;
                }
            }
            catch
            {
                Console.WriteLine("Could not retreive Sub Tier Name from Database.");
                return null;
            }
        }
        return user.ElementAt(0).SubTierName;
    }

    public async Task<string> GetSubTierNameFromCustomerID(string _customerID)
    {
        if (!ServerStatusManager.ServerOnline)
        {
            return null;
        }

        string sql = "select SubTierName from user where CustomerID = @CustomerID";

        try
        {
            user = await _data.LoadData<UserModel, dynamic>(sql, new { CustomerID = _customerID }, _config.GetConnectionString("default"));
            if (user != null)
            {
                CurrentUser.SubTierName = user.ElementAt(0).SubTierName;
                return user.ElementAt(0).SubTierName;
            }
        }
        catch
        {
            Console.WriteLine("Could not retreive Sub Tier Name from Database.");
            return null;
        }
        return user.ElementAt(0).SubTierName;
    }


    public async Task<int> GetLifetimeSubFromDB()
    {
        if (!ServerStatusManager.ServerOnline)
        {
            return 0;
        }

        string sql = "select LifetimeSub from user where UserName = @UserName";

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client Null at GetLifetimeSubFromDB");
            return 0;
        }
        else
        {
            try
            {
                user = await _data.LoadData<UserModel, dynamic>(sql, new { UserName = Auth0Client.UserName }, _config.GetConnectionString("default"));
                if (user != null)
                {
                    CurrentUser.LifetimeSub = user.ElementAt(0).LifetimeSub;
                    return user.ElementAt(0).LifetimeSub;
                }
            }
            catch
            {
                Console.WriteLine("Could not retreive Lifetime Sub from Database.");
                return 0;
            }
        }
        return user.ElementAt(0).LifetimeSub;
    }
    public async Task<string> GetAccessLevelFromDB()
    {
        if (!ServerStatusManager.ServerOnline)
        {
            return null;
        }

        string sql = "select AccessLevel from user where UserName = @UserName";

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client Null at GetAccessLevelFromDB");
            return null;
        }
        else
        {
            try
            {
                user = await _data.LoadData<UserModel, dynamic>(sql, new { UserName = Auth0Client.UserName }, _config.GetConnectionString("default"));
                if (user != null)
                {
                    CurrentUser.AccessLevel = user.ElementAt(0).AccessLevel;
                    return user.ElementAt(0).AccessLevel;
                }
            }
            catch
            {
                Console.WriteLine("Could not retreive Access Level from Database.");
                return null;
            }
        }
        return user.ElementAt(0).AccessLevel;
    }

    //SET FUNCTIONS  

    public async Task<Boolean> InsertNewSubscriptionIDInDB(string _subscriptionID)
    {
        if(!ServerStatusManager.ServerOnline)
        {
            return false;
        }

        UpdateDatabaseStatus();

        if (!DatabaseOnline)
        {
            return false;
        }

        string sql = "update user set SubscriptionID = @SubscriptionID where UserName = @UserName";

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client Null at UpdateSubscriptionIDInDB");
            return false;
        }
        else
        {
            try
            {
                await _data.SaveData(sql, new { UserName = Auth0Client.UserName, SubscriptionID = _subscriptionID }, _config.GetConnectionString("default"));
                Console.WriteLine("Updated SubscriptionID");
                return true;
            }
            catch
            {
                Console.WriteLine("Could not update SubscriptionID in Database.");
                return false;
            }
        }
        return true;
    }

    public async Task<Boolean> InsertNewUpgradeIDInDB(string _subscriptionID)
    {
        if(!ServerStatusManager.ServerOnline)
        {
            return false;
        }

        UpdateDatabaseStatus();

        if (!DatabaseOnline)
        {
            return false;
        }

        string sql = "update user set UpgradeID = @UpgradeID where UserName = @UserName";

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client Null at InsertNewUpgradeIDInDB");
            return false;
        }
        else
        {
            try
            {
                await _data.SaveData(sql, new { UserName = Auth0Client.UserName, UpgradeID = _subscriptionID }, _config.GetConnectionString("default"));
                Console.WriteLine("Updated UpgradeID");
                return true;
            }
            catch
            {
                Console.WriteLine("Could not update UpgradeID in Database.");
                return false;
            }
        }
        return true;
    }

    public async Task UpdateAccessLevelInDB(string tierName)
    {
        if(!ServerStatusManager.ServerOnline)
        {
            return;
        }

        UpdateDatabaseStatus();

        if (!DatabaseOnline)
        {
            return;
        }

        string sql = "update user set AccessLevel = @AccessLevel where UserName = @UserName";

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client Null at UpdateAccessLevelInDB");
            return;
        }
        else
        {
            try
            {
                await _data.SaveData(sql, new { UserName = Auth0Client.UserName, AccessLevel = tierName }, _config.GetConnectionString("default"));
                Console.WriteLine("Updated Client Access Level.");
                return;
            }
            catch
            {
                Console.WriteLine("Could not update Client Access Level in Database.");
                return;
            }
        }
    }
    public async Task UpdateSubTierInDB(int tierNumber)
    {
        if (!ServerStatusManager.ServerOnline)
        {
            return;
        }

        string sql = "update user set SubTier = @SubTier where UserName = @UserName";

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client Null at UpdateSubTierInDB");
            return;
        }
        else
        {
            try
            {
                await _data.SaveData(sql, new { UserName = Auth0Client.UserName, SubTier = tierNumber }, _config.GetConnectionString("default"));
                Console.WriteLine("Updated Sub Tier in Database.");
                return;
            }
            catch
            {
                Console.WriteLine("Could not update Sub Tier in Database.");
                return;
            }
        }
    }
    public async Task UpdateSubTierNameInDB(string tierName)
    {
        if (!ServerStatusManager.ServerOnline)
        {
            return;
        }

        string sql = "update user set SubTierName = @SubTierName where UserName = @UserName";

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client Null at UpdateSubTierNameInDB");
            return;
        }
        else
        {
            try
            {
                await _data.SaveData(sql, new { UserName = Auth0Client.UserName, SubTierName = tierName }, _config.GetConnectionString("default"));
                Console.WriteLine("Updated Sub Tier name in Database.");
                return;
            }
            catch
            {
                Console.WriteLine("Could not update Sub Tier Name in Database.");
                return;
            }
        }

    }
    public async Task UpdateCustomerIDInDB(string _customerID)
    {
        if(!ServerStatusManager.ServerOnline)
        {
            return;
        }

        UpdateDatabaseStatus();

        if (!DatabaseOnline)
        {
            return;
        }

        string sql = "update user set CustomerID = @CustomerID where UserName = @UserName";

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client Null at UpdateCustomerIDInDB");
            return;
        }
        else
        {
            try
            {
                await _data.SaveData(sql, new { UserName = Auth0Client.UserName, CustomerID = _customerID }, _config.GetConnectionString("default"));
                Console.WriteLine("Updated CustomerID");
                return;
            }
            catch
            {
                Console.WriteLine("Could not update CustomerID in Database.");
                return;
            }
        }
    }

    public async Task<Boolean> UpdateSubscriptionIDInDB(string _subscriptionID)
    {
        if(!ServerStatusManager.ServerOnline)
        {
            return false;
        }

        UpdateDatabaseStatus();

        if (!DatabaseOnline)
        {
            return false;
        }

        string sql = "update user set SubscriptionID = @SubscriptionID where UserName = @UserName";

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client Null at UpdateSubscriptionIDInDB");
            return false;
        }
        else
        {
            try
            {
                await _data.SaveData(sql, new { UserName = Auth0Client.UserName, SubscriptionID = _subscriptionID }, _config.GetConnectionString("default"));
                Console.WriteLine("Updated SubscriptionID");
                return true;
            }
            catch
            {
                Console.WriteLine("Could not update SubscriptionID in Database.");
                return false;
            }
        }
        return true;
    }

    public async Task<int> UpdatePaidStatusInDB(int isPaid)
    {
        if(!ServerStatusManager.ServerOnline)
        {
            return -1;
        }

        UpdateDatabaseStatus();

        if (!DatabaseOnline)
        {
            return -1;
        }

        string sql = "update user set PaidStatus = @PaidStatus where UserName = @UserName";

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client Null at UpdatePaidStatusFromDB");
            return -1;
        }
        else
        {
            try
            {
                await _data.SaveData(sql, new { UserName = Auth0Client.UserName, PaidStatus = isPaid }, _config.GetConnectionString("default"));
                Console.WriteLine("Updated PaidStatus");
                return 1;
            }
            catch
            {
                Console.WriteLine("Could not update Paid Status in Database");
                return -1;
            }
        }
        return -1;
    }

    public async Task SetNewCustomerDateInDB(DateTime _newCustomerDate)
    {
        if (!ServerStatusManager.ServerOnline)
        {
            return;
        }

        string sql = "update user set NewCustomerDate = @NewCustomerDate where UserName = @UserName";

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client Null at SetNewCustomerDateInDB");
            return;
        }
        else
        {
            try
            {
                await _data.SaveData(sql, new { UserName = Auth0Client.UserName, NewCustomerDate = _newCustomerDate }, _config.GetConnectionString("default"));
                Console.WriteLine("Updated NewCustomerDate");
                return;
            }
            catch
            {
                Console.WriteLine("Could not update Paid Status in Database");
                return;
            }
        }
        return;
    }

    public async Task<DateTime> GetNewCustomerDateFromDB()
    {
        if (!ServerStatusManager.ServerOnline)
        {
            return DateTime.MinValue;
        }

        string sql = "select NewCustomerDate from user where UserName = @UserName";

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client Null at GetLifetimeSubFromDB");
            return DateTime.MinValue;
        }
        else
        {
            try
            {
                user = await _data.LoadData<UserModel, dynamic>(sql, new { UserName = Auth0Client.UserName }, _config.GetConnectionString("default"));
                if (user != null)
                {
                    CurrentUser.NewCustomerDate = user.ElementAt(0).NewCustomerDate;
                    return user.ElementAt(0).NewCustomerDate;
                }
            }
            catch
            {
                Console.WriteLine("Could not retreive Lifetime Sub from Database.");
                return DateTime.MinValue;
            }
        }
        return user.ElementAt(0).NewCustomerDate;
    }


    #endregion


    #region CORE DATABASE FUNCTIONS

    //CORE DATABASE FUNCTIONS
    public async Task InsertData()
    {
        string sql = "insert into user (Username, CustomerID) values (@UserName, @CustomerID)";

        await _data.SaveData(sql, new { Username = "", CustomerID = "" }, _config.GetConnectionString("default"));

    }

    public async Task UpdateData()
    {
        string sql = "update user set Username = @UserName where UserName = @UserName";

        await _data.SaveData(sql, new { UserName = "" }, _config.GetConnectionString("default"));

    }

    public async Task DeleteData()
    {
        string sql = "delete from user where UserName = @UserName";

        await _data.SaveData(sql, new { UserName = "" }, _config.GetConnectionString("default"));

    }

    #endregion


    #region TEST DB FUNCTIONS
    //Test DB Functions


    public async Task InsertTestUser(string email, string username, string userid)
    {

        string sql = "insert into user (UserID, Email, UserName) values (@UserID, @Email, @UserName)";

        await _data.SaveData(sql, new { UserID = userid, Email = email, UserName = username }, _config.GetConnectionString("default"));
    }

    #endregion


    #region Stripe

    ////STRIPE////

    public StripeList<Subscription> Subscription;

    public string StripeSubscription;
    
    public string StripeCustomers;

    public string StripeCustomer;
    public string CustomerID { get; set; }
    public string SubscriptionStatus { get; set; }
    public string SubscriptionCancellationStatus { get; set; }
    public string SubscriptionTimeLeft { get; set; }
    public string SubscriptionID { get; set; }


    //Customer and Subscription Functions
    public async Task<string> CreateStripeCustomer(string _email)
    {
        var options = new CustomerCreateOptions
        {
            Email = _email
        };

        var service = new CustomerService();
        var customer = service.Create(options);
        //Console.WriteLine(customer);
        StripeCustomer = customer.ToString();
        return customer.Id;
    }
    
    public async Task GetStripeCustomerList()
    {
        var service = new CustomerService();
        var customers = service.List();
        Console.WriteLine(customers);
        StripeCustomers = customers.ToString();
    }

    
    public async Task GetStripeCustomerByID(string _customerID)
    {
        var service = new CustomerService();
        var customer = service.Get(_customerID);
        Console.WriteLine(customer);
        StripeCustomer = customer.ToString();
    }

    public async Task<string> GetStripeCustomerFromEmail(string _email)
    {
        var options = new CustomerListOptions
        {
            Email = _email
        };
        var service = new CustomerService();
        StripeList<Customer> customers = service.List(
          options
        );

        StripeCustomer = customers.FirstOrDefault().Id;
        Console.WriteLine(StripeCustomer);
        return StripeCustomer;
    }

    public async Task<Boolean?> EmailVerificationCheck()
    {
        bool? status;

        if (Auth0Client == null)
        {
            Console.WriteLine("Email Check - Auth0Client = null");
            return false;
        }

        Console.WriteLine("Polling Auth0Client.EmailVerified");
        status = Auth0Client.EmailVerified;
        return status;
    }




    public async Task<StripeList<Subscription>> GetStripeSubscription(string _customerID)
    {
        var options = new SubscriptionListOptions
        {
            Customer = _customerID
        };
        CustomerID = _customerID;

        var service = new SubscriptionService();
        Subscription = service.List(options);
        //StripeSubscription = Subscription.ToString();

        if(Subscription != null)
        {
            //SubscriptionID = Subscription.FirstOrDefault().Id;
            //SubscriptionStatus = Subscription.FirstOrDefault().Status.ToString().ToUpper();
            //SubscriptionTimeLeft = Subscription.FirstOrDefault().CurrentPeriodEnd.ToString();
            //SubscriptionCancellationStatus = Subscription.FirstOrDefault().CanceledAt.ToString();
            return Subscription;
        }
        else
        {
            return null;
        }        
    }

    public async Task<StripeList<Subscription>> GetSubscriptionByTier(string _customer, string _tierID)
    {

        var options = new SubscriptionListOptions
        {
            Customer = _customer,
            Price = _tierID,
        };
        var service = new SubscriptionService();
        StripeList<Subscription> subscriptions = service.List(
          options
        );
        return subscriptions;
    }

    public async Task<Subscription> GetSubscriptionByID(string _subscriptionID)
    {
        var service = new SubscriptionService();
        var subscription = service.Get(_subscriptionID);
        return subscription;
    }

    public async Task SetSubCancelStatus()
    {
        await GetStripeSubscription(CustomerID);

        if (Subscription.FirstOrDefault().Status.ToString().ToUpper() == "ACTIVE" && Subscription.FirstOrDefault().CancelAt == null)
        {
            SubscriptionStatus = "ACTIVE";
        }
        if (Subscription.FirstOrDefault().Status.ToString().ToUpper() == "ACTIVE" && Subscription.FirstOrDefault().CancelAt != null)
        {
            SubscriptionStatus = "ACTIVE (Pending Cancellation)";
        }
    }

    public async Task<string> GetSubStatusFromStripe()
    {
        try
        {
            var CustomerID = await GetCustomerIDFromDB();

            if(CustomerID == null)
            {
                return null;
            }

            await GetStripeSubscription(CustomerID);

            if(Subscription.Data.Count == 0)
            {
                return null;
            }
            else
            {
                if (Subscription.FirstOrDefault().Status.ToString().ToUpper() == "ACTIVE" && Subscription.FirstOrDefault().CancelAt == null)
                {
                    SubscriptionStatus = "ACTIVE";
                }
                if (Subscription.FirstOrDefault().Status.ToString().ToUpper() == "ACTIVE" && Subscription.FirstOrDefault().CancelAt != null)
                {
                    SubscriptionStatus = "ACTIVE (Pending Cancellation)";
                }
                if (Subscription.FirstOrDefault().Status.ToString().ToUpper() == "INCOMPLETE" && Subscription.FirstOrDefault().CancelAt == null)
                {
                    SubscriptionStatus = "INCOMPLETE (Payment Failed)";
                }
                if (Subscription.FirstOrDefault().Status.ToString().ToUpper() == "INCOMPLETE_EXPIRED" && Subscription.FirstOrDefault().CancelAt == null)
                {
                    SubscriptionStatus = "INCOMPLETE (Invoice Expired - Contact Billing)";
                }
                if (Subscription.FirstOrDefault().Status.ToString().ToUpper() == "TRIALING" && Subscription.FirstOrDefault().CancelAt == null)
                {
                    SubscriptionStatus = "TRIAL";
                }
                if (Subscription.FirstOrDefault().Status.ToString().ToUpper() == "PAST_DUE" && Subscription.FirstOrDefault().CancelAt == null)
                {
                    SubscriptionStatus = "PAST DUE (Payment Not Received)";
                }
                if (Subscription.FirstOrDefault().Status.ToString().ToUpper() == "UNPAID" && Subscription.FirstOrDefault().CancelAt == null)
                {
                    SubscriptionStatus = "UNPAID (Contact Billing)";
                }
            }
            return SubscriptionStatus;
        }
        catch
        {
            return null;
        }
        return null;
    }

    public async Task<Boolean> ResetAccessToDefaults(string _customerID)
    {
        if(!ServerStatusManager.ServerOnline)
        {
            return false;
        }

        UpdateDatabaseStatus();

        if (!DatabaseOnline)
        {
            return false;
        }

        string sql = "update user set AccessLevel = @AccessLevel, SubTier = @SubTier, SubTierName = @SubTierName, PaidStatus = @PaidStatus, SubscriptionID = @SubscriptionID where CustomerID = @CustomerID";
               
        try
        {
            await _data.SaveData(sql, new { CustomerID = _customerID, AccessLevel = "Bolt Action", SubTier = 0, SubTierName = "Bolt Action", PaidStatus = 0, SubscriptionID = "" }, _config.GetConnectionString("default"));
            Console.WriteLine("Reset to Defaults in Database");
            return true;
        }
        catch
        {
            Console.WriteLine("Could not Reset Access to Defaults in Database.");
            return false;
        }
        return false;
    }

    public async Task<Boolean> TempDisableAccessInDB(string _customerID)
    {
        if(!ServerStatusManager.ServerOnline)
        {
            return false;
        }

        UpdateDatabaseStatus();

        if (!DatabaseOnline)
        {
            return false;
        }

        string sql = "update user set AccessLevel = @AccessLevel, PaidStatus = @PaidStatus where CustomerID = @CustomerID";

        try
        {
            await _data.SaveData(sql, new { CustomerID = _customerID, AccessLevel = "Bolt Action", PaidStatus = 0 }, _config.GetConnectionString("default"));
            Console.WriteLine("Temporarily Disabled Access in Database");
            return true;
        }
        catch
        {
            Console.WriteLine("Could not Temporarily Disable Access in Database.");
            return false;
        }
        return false;
    }

    public async Task<Boolean> ReEnableAccessInDB(string _customerID)
    {
        if(!ServerStatusManager.ServerOnline)
        {
            return false;
        }

        UpdateDatabaseStatus();

        if (!DatabaseOnline)
        {
            return false;
        }

        string sql = "update user set AccessLevel = @AccessLevel, PaidStatus = @PaidStatus where CustomerID = @CustomerID";

        try
        {
            await _data.SaveData(sql, new { CustomerID = _customerID, AccessLevel = await GetSubTierNameFromCustomerID(_customerID), PaidStatus = 1 }, _config.GetConnectionString("default"));
            Console.WriteLine("ReEnabled Access in Database");
            return true;
        }
        catch
        {
            Console.WriteLine("Could not ReEnable Access in Database.");
            return false;
        }
        return false;
    }


    public async Task CancelSubscriptionImmediate(string _subscriptionID)
    {
        var options = new SubscriptionCancelOptions
        {
            Prorate = true,
            InvoiceNow = true,
        };
        
        var service = new SubscriptionService();
        service.Cancel(_subscriptionID, options);
    }


    public async Task CancelSubscriptionAtPeriodEnd(string _subscriptionID)
    {
        var options = new SubscriptionUpdateOptions
        {
            CancelAtPeriodEnd = true,
        };
        var service = new SubscriptionService();
        var subscription = service.Update(_subscriptionID, options);
        await Task.Delay(1000);
        await SetSubCancelStatus();
    }

    public async Task ReActivateCancelledSubscription(string _subscriptionID)
    {
        var options = new SubscriptionUpdateOptions
        {
            CancelAtPeriodEnd = false,
        };
        var service = new SubscriptionService();
        var subscription = service.Update(_subscriptionID, options);
        await Task.Delay(1000);
        await SetSubCancelStatus();
    }

    public async Task<DateTime> GetSubscriptionPeriod(Subscription _subscription)
    {
        try
        {
            return _subscription.CurrentPeriodEnd;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    public async Task<string> GetSubscriptionPlan(Subscription _subscription)
    {
        if(_subscription == null)
        {
            return null;
        }

        try
        {
            if (_subscription.Items.FirstOrDefault().Plan.Id == "price_1JaFsOA9Yey7WFTEuioJsB7J")
            {
                return "Semi-Auto";
            }
            if (_subscription.Items.FirstOrDefault().Plan.Id == "price_1JaFtqA9Yey7WFTEgC7FHaek")
            {
                return "Full Auto";
            }
            if (_subscription.Items.FirstOrDefault().Plan.Id == "price_1JaFv5A9Yey7WFTEqVt9UjrG")
            {
                return "Belt Fed";
            }
            else
            {
                return null;
            }
        }
        catch
        {
            Console.WriteLine("Could not find plan ID from Old Subscription");
            return null;
        }
        return null;
    }


    //Create Checkout Session

    public async Task<string> CreateCheckoutSessionAsync(string priceId, string customerId)
    {
        if(!ServerStatusManager.ServerOnline)
        {
            return null;
        }

        await UpdateDatabaseStatus();
        if(!DatabaseOnline)
        {
            return null;
        }


        var options = new SessionCreateOptions
        {
            Customer = customerId,
            PaymentMethodTypes = new List<string>
                {
                    "card",
                },
            LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1
                    },
                },
            Mode = "subscription",
            //SuccessUrl = $"http://localhost:7432/Stripe-Payment-Success/" + GUIDHash + "?session_id={{CHECKOUT_SESSION_ID}}",    //LOCAL HOST
            //CancelUrl = $"http://localhost:7432/Stripe-Payment-Cancel/" + GUIDHash,


            SuccessUrl = $"https://www.taxov.tax/Stripe-Payment-Success/" + GUIDHash + "?session_id={{CHECKOUT_SESSION_ID}}",     //VPS SERVER
            CancelUrl = $"https://www.taxov.tax/Stripe-Payment-Cancel/" + GUIDHash,

        };

        return (await new SessionService().CreateAsync(options)).Id;
    }


    #endregion

    #region Settings Functions

    public async Task ToggleToolTipsInDB(bool _isActive)
    {
        if (!ServerStatusManager.ServerOnline)
        {
            return;
        }

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client Null at ToggleToolTipsinDB");
            return;
        }

        string sql = "update usersettings set ToolTips = @ToolTips where UserName = @UserName";

        try
        {
            await _data.SaveData(sql, new { UserName = Auth0Client.UserName, ToolTips = _isActive }, _config.GetConnectionString("default"));
            Console.WriteLine("Toggled ToolTip Settings in DB");
            return;
        }
        catch
        {
            Console.WriteLine("Could not Toggle ToolTips Settings in Database.");
            return;
        }
        return;
    }

    public async Task ToggleNewsletterInDB(bool _isActive)
    {
        if (!ServerStatusManager.ServerOnline)
        {
            return;
        }

        int NewsletterState = 0;

        if(_isActive)
        {
            NewsletterState = 1;
        }
        if(!_isActive)
        {
            NewsletterState = 0;
        }

        if (Auth0Client == null || Auth0Client.UserName == null)
        {
            Console.WriteLine("Auth0Client Null at ToggleNewsletterinDB");
            return;
        }

        string sql = "update usersettings set Newsletter = @Newsletter where UserName = @UserName";

        try
        {
            await _data.SaveData(sql, new { UserName = Auth0Client.UserName, Newsletter = NewsletterState }, _config.GetConnectionString("default"));
            Console.WriteLine("Toggled Newsletter Settings in DB");
            return;
        }
        catch
        {
            Console.WriteLine("Could not Toggle Newsletter Settings in Database.");
            return;
        }
        return;
    }


    #endregion


















    //Shitty Test Functions
    public async Task FuckUp(int[] number)
    {
        var intarray = number[5];
    }

    public async Task TestWebHook()
    {
        //Console.WriteLine("CW: Tested Webhook");
        Debug.WriteLine("Payment Intent Succeeded - 200OK");
    }

    public async Task TestSubDeleted(string custID, string subID)
    {
        Console.WriteLine(custID);
        Console.WriteLine(subID);
    }

}