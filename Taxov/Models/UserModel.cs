using System;

namespace Taxov.Models
{
    public class UserModel
    {
        public int ClientID { get; set; }
        public string Email { get; set; }
        public string UserID { get; set; }
        public string UserName { get; set; }
        public string CustomerID { get; set; }
        public string SubscriptionID { get; set; }
        public int PaidStatus { get; set; }
        public DateTime SubTimeRemaining { get; set; }
        public int SubTier { get; set; }
        public string SubTierName { get; set; }
        public int LifetimeSub { get; set; }
        public string AccessLevel { get; set; }
        public int AccountStatus { get; set; } //Banned , Permanent, etc..
        public DateTime NewCustomerDate { get; set; }
        public int ToolTips { get; set; }
        public int Sound { get; set; }
        public int LevelThreeIntel { get; set; }
        public int Newsletter { get; set; }


    }
}
