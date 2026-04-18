using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using DataLibrary;
using Microsoft.AspNetCore.Components.Authorization;
using Stripe;
using System.Net;
using System.IO;

namespace Taxov
{
    public class Startup
    {
        public bool DisallowsSameSiteNone(string userAgent)
        {
            // Check if a null or empty string has been passed in, since this
            // will cause further interrogation of the useragent to fail.
            if (String.IsNullOrWhiteSpace(userAgent))
                return false;

            // Cover all iOS based browsers here. This includes:
            // - Safari on iOS 12 for iPhone, iPod Touch, iPad
            // - WkWebview on iOS 12 for iPhone, iPod Touch, iPad
            // - Chrome on iOS 12 for iPhone, iPod Touch, iPad
            // All of which are broken by SameSite=None, because they use the iOS networking
            // stack.
            if (userAgent.Contains("CPU iPhone OS 12") ||
                userAgent.Contains("iPad; CPU OS 12"))
            {
                return true;
            }

            // Cover Mac OS X based browsers that use the Mac OS networking stack. 
            // This includes:
            // - Safari on Mac OS X.
            // This does not include:
            // - Chrome on Mac OS X
            // Because they do not use the Mac OS networking stack.
            if (userAgent.Contains("Macintosh; Intel Mac OS X 10_14") &&
                userAgent.Contains("Version/") && userAgent.Contains("Safari"))
            {
                return true;
            }

            // Cover Chrome 50-69, because some versions are broken by SameSite=None, 
            // and none in this range require it.
            // Note: this covers some pre-Chromium Edge versions, 
            // but pre-Chromium Edge does not require SameSite=None.
            if (userAgent.Contains("Chrome/5") || userAgent.Contains("Chrome/6"))
            {
                return true;
            }

            return false;
        }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        private void CheckSameSite(HttpContext httpContext, CookieOptions options)
        {
            if (options.SameSite == SameSiteMode.None)
            {
                var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
                if (DisallowsSameSiteNone(userAgent))
                {
                    options.SameSite = SameSiteMode.Unspecified;
                }
            }
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddServerSideBlazor().AddCircuitOptions(options => { options.DetailedErrors = true; });
            services.AddSingleton<HttpClient>();
            services.AddSingleton<IDataAccess, DataAccess>();
            services.AddSingleton<Data.TokenManager>();
            services.AddSingleton<Data.ServerStatusManager>();
            services.AddSingleton<Data.Navigation>();
            services.AddSingleton<Data.TaxCalculator>();
            

            services.AddScoped<Data.Authentication>();

            services.AddAuthentication(options => {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options => {
            options.ExpireTimeSpan = new TimeSpan(0, 2, 0);
             })

            .AddOpenIdConnect("Auth0", options => {
            options.Authority = $"https://{Configuration["Auth0:Domain"]}";               

            options.ClientId = Configuration["Auth0:ClientId"];
            options.ClientSecret = Configuration["Auth0:ClientSecret"];


            options.ResponseType = OpenIdConnectResponseType.Code;

            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");   // <- Optional extra

            options.CallbackPath = new PathString("/callback");
            options.ClaimsIssuer = "Auth0";
            options.SaveTokens = true;

            options.TokenValidationParameters = new()
            {
                NameClaimType = "name",
                };


                // Add handling of lo
                options.Events = new OpenIdConnectEvents
            {
                OnRedirectToIdentityProviderForSignOut = (context) =>
                {
                    var logoutUri = $"https://{Configuration["Auth0:Domain"]}/v2/logout?client_id={Configuration["Auth0:ClientId"]}";

                    var postLogoutUri = context.Properties.RedirectUri;
                    if (!string.IsNullOrEmpty(postLogoutUri))
                    {
                        if (postLogoutUri.StartsWith("/"))
                        {
                            var request = context.Request;
                            postLogoutUri = request.Scheme + "://" + request.Host + request.PathBase + postLogoutUri;
                        }
                        logoutUri += $"&returnTo={ Uri.EscapeDataString(postLogoutUri)}";
                    }

                    context.Response.Redirect(logoutUri);
                    context.HandleResponse();

                    return Task.CompletedTask;
                }
            };
        });

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.OnAppendCookie = cookieContext =>
                    CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
                options.OnDeleteCookie = cookieContext =>
                    CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }            

            app.UseRouting();

            app.UseCookiePolicy();

            app.UseAuthentication();

            app.UseAuthorization();

            StripeConfiguration.ApiKey = Configuration.GetSection("Stripe")["ApiKey"];            

            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    if (ctx.Context.Request.Path == "/RSSDisplay/RSSFeed.txt")
                    {
                        ctx.Context.Response.Headers.Add("Cache-Control", "no-store");
                        ctx.Context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        ctx.Context.Response.ContentLength = 0;
                        ctx.Context.Response.Body = Stream.Null;
                        ctx.Context.Response.Redirect("/");
                    }
                    if (ctx.Context.Request.Path == "/RSSDisplay/RSSError.txt")
                    {
                        ctx.Context.Response.Headers.Add("Cache-Control", "no-store");
                        ctx.Context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        ctx.Context.Response.ContentLength = 0;
                        ctx.Context.Response.Body = Stream.Null;
                        ctx.Context.Response.Redirect("/");
                    }
                }
            });

            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();                               
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
