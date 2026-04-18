using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Stripe;
using Taxov.Data;

namespace Taxov.Controllers
{
	


	[Route("taxovhook")]
	public class StripeWebHook : ControllerBase
	{
		private const string _webhookSecret = "XXXXXXXXXXXXXXXXXXXXXX";

		private readonly Authentication Authentication;


		public StripeWebHook(Authentication _authentication)
        {
			Authentication = _authentication;
        }
		
		[HttpPost]
		public async Task<IActionResult> Index()
		{
			string json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

			try
			{
				var stripeEvent = EventUtility.ConstructEvent(json,
					Request.Headers["Stripe-Signature"], _webhookSecret);
				switch (stripeEvent.Type)
				{
					case Events.CustomerSubscriptionDeleted:
                        {
							var oldSubscription = stripeEvent.Data.Object as Subscription;

							var customerID = oldSubscription.CustomerId;
							var subscriptionID = oldSubscription.Id;

                            var currentSubscriptions = await Authentication.GetStripeSubscription(customerID);

							//Check if No Active Subscriptions - If So, Reset Database and Customer Access
							if(currentSubscriptions == null)
                            {
								await Authentication.ResetAccessToDefaults(customerID);
								return Ok();
                            }

							//Check if Active Subscriptions and How Many
							//If More than One, Upgrade is in Progress and let Payment Wall process normally
							//If Only one Active Sub, Check if it's ID is new
							//If ID matches old Subscription, Reset Database and Customer Access
							if(currentSubscriptions != null)
                            {
								if(currentSubscriptions.Data.Count > 1)
                                {
									Console.WriteLine("Upgrade in Progress...");
									return Ok();
                                }
                                else
                                {
									if(currentSubscriptions.FirstOrDefault().Id != subscriptionID)
                                    {
										Console.WriteLine("Upgraded Subscription.");
										return Ok();
                                    }
									else										
                                    {
										await Authentication.ResetAccessToDefaults(customerID);
									}
                                }
                            }
							return Ok();							
						}

					case Events.CustomerSubscriptionUpdated:
                        {
							var Subscription = stripeEvent.Data.Object as Subscription;
							var previousSubscriptionState = stripeEvent.Data.PreviousAttributes as Subscription;

							var customerID = Subscription.CustomerId;
							var subscriptionID = Subscription.Id;

							var currentStatus = Subscription.Status;
							var previousStatus = previousSubscriptionState.Status;

                            //Check for change to Past Due Status
                            try
                            {
								if(previousStatus != null && currentStatus != null)
                                {
									if (previousStatus == "active" && currentStatus == "past_due")
									{
										//Temporarily Disable Access until Payment Succeeds
										await Authentication.TempDisableAccessInDB(customerID);
										return Ok();
									}
								}
                            }
                            catch
                            {
								return Ok();
                            }

							//Check for change to Active Status
                            try
                            {
								if (previousStatus != null && currentStatus != null)
								{
									if (previousStatus == "past_due" && currentStatus == "active")
									{
										//Re-Enable Access
										await Authentication.ReEnableAccessInDB(customerID);
										return Ok();
									}
								}
							}
							catch
                            {
								return Ok();
                            }

							return Ok();
						}


					case Events.PaymentIntentSucceeded:
                        {
							//Testing Webhook
							await Authentication.TestWebHook();
							return Ok();
						}
				}
				return Ok();
			}
			catch (StripeException e)
			{
				return BadRequest();
			}
		}

	}
}
