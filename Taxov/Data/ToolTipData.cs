namespace Taxov.Data
{
    public static class ToolTipData
    {

        //
        //INPUTS
        //

        public static string VendorSelection()                           { return "Choose the vendor with the highest buyback price"; }
        public static string VendorBuyBack()                            { return "Input highest price a vendor will buy an item"; } 
        public static string DesiredListPrice()                         { return "Input desired list price for your item(s)"; }
        public static string Quantity()                                 { return "Input number of items to be sold"; }
        public static string BulkSale()                                 { return "Check if all items will be sold in Bulk"; }
        public static string HideoutMgmtLevel()                         { return "Input Hideout Management Level.  If left blank, Level 3 Discount defaults to 30%"; }
        public static string LevelThreeIntel()                          { return "Check if Intelligence Center Level (3) is unlocked"; }
        public static string MarketPrice()                              { return "Input market price.  Example: Market average, Selling point, or Lowest listing"; }
        public static string Investments()                              { return "Input cost of production or procurement.  If found in raid, leave blank"; }


        //
        //PROPRIETARY STATS
        //

        public static string BaseValue()                                { return "Predetermined algorithmic value"; }
        public static string BreakEven()                                { return "Minimum list price after taxes deducted to Net Zero +/- 1&#x20bd;"; }
        public static string UpperLimit()                              { return "Maximum list price before profit / flea reputation stalls and reverts towards zero"; }
        public static string LowestTaxPercentage()                      { return "Lowest tax % price.  Median between High and Low Margins"; }
        public static string MarginHigh()                               { return "Maximum list price before tax rates heavily increase"; }
        public static string MarginLow()                                { return "Minimum list price before tax rates heavily increase"; }
        public static string Valuation()                                { return "Evaluation of Market Buyer &#40;&lt;&nbsp;1&#41; &nbsp;&nbsp; Seller &#40;&gt;&nbsp;1&#41;"; }
        public static string FleaRepGains()                             { return "Flea reputation gain for every 50,000 in Realized Gains"; }


        //
        //TAX BREAKDOWN
        //

        public static string EstTaxes()                                 { return "Estimated Total Taxes.  Accuracy within +/- 100 Roubles"; }
        public static string TaxPerItem()                               { return "Taxes Per Item when quantity &#40;&gt;&nbsp;1&#41;"; }
        public static string RealizedGains()                            { return "Total Profit earned after investments and taxes deducted"; }
        public static string StandardTax()                              { return "Standard Taxes rates without discounts"; }
        public static string DiscountTax()                              { return "Discounted Taxes rates from Intel Center Level (3)"; }
        public static string DiscountSavings()                          { return "Taxes Saved from Intel Center Level (3) discount"; }
        public static string TaxPercentOfBase()                         { return "Percentage paid in Tax of Base Value.  Ratio of Tax &gt; Value"; }
        public static string PercentPaidInTax()                         { return "Percentage paid in Tax along the Taxov Bell Curve"; }
        

        //
        //SALE ANALYSIS
        //

        public static string SaleGross()                                { return "Unadjusted gross earnings from sale"; }
        public static string SaleNet()                                  { return "Net earnings after taxes deducted"; }
        public static string PerItemProfitOverVendor()                  { return "Profit earned over selling the item to vendor"; }
        public static string PerItemProfitGross()                       { return "Net earnings per item after taxes deducted"; }
        public static string PerItemProfitRealized()                    { return "Total Profit earned per item after investments and taxes deducted"; }


        //
        //VENDOR CHART
        //
                
        public static string VendorChart_BreakEven()                    { return "Mark Up List Price over Break Even Price"; }
        public static string VendorChart_BreakEvenGains()               { return "Profit over Break Even Price"; }
        public static string VendorChart_BaseValue()                    { return "Mark Up List Price over Item Base Value"; }
        public static string VendorChart_BaseValueGains()               { return "Profit over Item Base Value"; }



        //
        //MARKET CHART
        //
               
        public static string MarketChart_MarketPrice()                  { return "List Price vs Market Price"; }
        public static string MarketChart_ProfitOverBreakEven()          { return "Profit over Break Even at List price"; }
        public static string MarketChart_ProfitOverBaseValue()          { return "Profit over Base Value at List price"; }
        public static string MarketChart_Valuation()                    { return "Evaluation of Market - Buyer &#40;&lt;&nbsp;1&#41; &nbsp;&nbsp; Seller &#40;&gt;&nbsp;1&#41;"; }
        public static string MarketChart_GainOrLoss()                   { return "Whether listing at this price will be a Gain or Loss"; }



    }
}
