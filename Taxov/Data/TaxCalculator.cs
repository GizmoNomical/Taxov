using System;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Taxov.Data
{
    public class TaxCalculator : ComponentBase
    {

        #region User Inputs

        //User Inputs

        protected int VendorSelector { get; set; } = 1;
        protected int VendorBuyBack { get; set; } = 0;
        protected int DesiredListPrice { get; set; } = 0;
        protected int MarketPrice { get; set; } = 0;
        protected int Investments { get; set; } = 0;
        protected int ItemQuantity { get; set; } = 1;
        protected bool BulkSale { get; set; } = false;
        protected bool BulkCheckBox { get; set; } = false;
        protected bool IntelDiscount { get; set; } = false;
        protected bool IntelCheckBox { get; set; } = false;
        protected int HideoutMgmtLevel { get; set; } = 0;

        #endregion

        #region Working Variables

        //Working Variables
        public double _BasePrice = 0;
        public double _TotalBaseValue = 0;
        public int _QuantityFactor = 1;
        public double _PO = 0;
        public double _PR = 0;
        public double _4PO = 0;
        public double _4PR = 0;
        public double _PerItemProfit_Gross = 0;
        public double _PerItemProfit_Realized = 0;
        public double _TaxPerItemDisplay = 0;
        public double _DiscountPercentage = 30.0;
        public double _VendorCoefficient = 0.63;

        #endregion

        #region Coefficients

        //Coefficients
        private static readonly double _ExponentValue = 1.08;
        private static readonly double _TaxConstantTi = 0.09;
        private static readonly double _TaxConstantTr = 0.05;
        private static readonly double _BaseValueCoeff = 1.5873015873015873015873015873016;
        private static readonly double _BaseValueDiscountCoeff = 1.0937441643323996265172735760971;
        private static readonly double _BreakEvenCoeff = 1.3061002253931830072096311354991;
        //private static readonly double _BreakEvenDiscountCoeff = 0.868680961;
        private static readonly double _BreakEvenDiscountCoeff = 1.151170616;
        private static readonly double _MarkupCoeff = 3.9104454104454104454104454104454;
        private static readonly double _MarkdownCoeff = 1.2300762300762300762300762300762;
        private static readonly double _BaseBuyBackCoeff = 1.14093;
        //private static readonly double _LowestTaxCoeff = 1.5507632662951296341167918584929;
        private static readonly double _LowestTaxCoeff = 2.0789936964657107160082917827859;
        private static readonly double _UpperLimitCoeff = 39.7538;
        private static readonly double _DiscountBasePercentage = 0.3;
        private static readonly double _FleaRepIncrement = 50000;
        protected static readonly double[] VendorCoeff = { 0.63, 0.63, 0.62, 0.6, 0.56, 0.49998, 0.49748, 0.48998, 0.412369};
        protected static readonly double[] VendorBreakEvenCoeff = { 0.82284314199770529454206761536444, 0.82284314199770529454206761536444, 0.82042740787697326092773762274682, 0.81532276180133122394659045616522, 0.80390478096764113230744069786363, 0.78315374334931450697728077020827, 0.78217956052205786330047745851664, 0.77919834852899524823925591360694, 0.74236997503284313438767988564521 };
        protected static readonly double[] VendorBreakEvenDiscountCoeff = { 0.151170616, 0.151170616, .153793874, .159392556, .172196735, .196516859, .197693416, .201313982, .248672253 };
        protected static readonly double[] VendorDiscountIncrement = { -0.00064154286, -0.00064154286, -0.00065173409, -0.00067343452, -0.00072280176, -0.00081555534, -0.00082000924, -0.00083369608, -0.00101019484 };
        #endregion

        #region Output Variables

        //Output Variables
        public double VendorAccuracy = 99.99;
        public double _StandardTax = 0;
        public double _DiscountTax = 0;
        public double _TotalTax = 0;

        public double _TaxPerItem = 0;
        public double _DiscountSavings = 0;

        public double _TaxPercentOfBase = 0;
        public double _PercentPaidInTax = 0;

        public double _EstSaleAmount = 0;
        public double _SaleGross = 0;
        public double _SaleNet = 0;

        public double _FleaRepPercentGain = 0.0;
        public double _RealizedGains = 0;
        public double _Valuation = 0.00;

        public double _BreakEvenPrice = 0;
        public double _UpperLimitPrice = 0;
        public double _LowestTax = 0;

        public double _MarginHigh = 0;
        public double _MarginLow = 0;
        public double _PerItemProfit_OverVendor = 0;
        public double _PerItemProfit_GrossMinusTaxDisplay = 0;
        public double _PerItemProfit_NetDisplay = 0;

        #endregion

        #region Graph Variables

        //Graph Variables

        public double[] VendorGraphValue = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public double[] MarketGraphValue = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public string[] MarketGraphLossGain = { "?", "?", "?", "?", "?", "?", "?" };
        public bool MarketPriceEntered = false;
        public bool CoreInputs = false;

        #endregion

        #region Admin Functions
        //Admin Functions
        public async Task AutoCalculateAsync()
        {
            await Calculate();
        }

        protected async Task ToggleIntelDiscountAsync()
        {
            IntelDiscount = !IntelDiscount;
            IntelCheckBox = !IntelCheckBox;
            await Calculate();
        }

        protected async Task ToggleBulkSaleAsync()
        {
            BulkSale = !BulkSale;
            BulkCheckBox = !BulkCheckBox;
            await Calculate();
        }

        #endregion

        #region Input Check Functions

        //Input Check Functions
        private async Task<Boolean> CheckCoreInputs(int _itemQuantity, int _vendorBuyBack, int _desiredListPrice)
        {
            if (_itemQuantity <= 0 || _vendorBuyBack <= 0 || _desiredListPrice <= 0)
            {
                CoreInputs = false;
                return false;
            }
            else if (_itemQuantity > 0 && _vendorBuyBack > 0 && _desiredListPrice > 0)
            {
                CoreInputs = true;
                return true;
            }
            else
            {
                return false;
            }
        }        
        private async Task<Boolean> IsItemQuantityLessThanOne(int _itemQuantity)
        {
            if (_itemQuantity < 1)
            {
                ItemQuantity = 0;
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task<Boolean> IsVendorBuyBackLessThanOne(int _vendorBuyBack)
        {
            if (_vendorBuyBack < 1)
            {
                VendorBuyBack = 0;
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task<Boolean> IsDesiredListPriceLessThanOne(int _desiredListPrice)
        {
            if (_desiredListPrice < 1)
            {
                DesiredListPrice = 0;
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task<Boolean> IsVendorBuyBackGreaterThanZero(int _vendorBuyBack)
        {
            if (_vendorBuyBack > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task<Boolean> IsItemQuantityGreaterThanOrEqualToOne(int _itemQuantity)
        {
            if (ItemQuantity >= 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task<Boolean> IsDesiredListPriceGreaterThanZero(int _desiredListPrice)
        {
            if (DesiredListPrice > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private async Task<Boolean> CheckMarketPriceInRange(int _marketPrice)
        {
            if (_marketPrice <= 0)
            {
                MarketPriceEntered = false;
                MarketPrice = 0;
                return false;
            }
            else
            {
                MarketPriceEntered = true;
                return true;
            }
        }

        private async Task<Boolean> IsInvestmentsInRange(int _investments)
        {
            if (_investments <= 0)
            {
                Investments = 0;
                return true;
            }
            else
            {
                return true;
            }          

            Investments = 0;
            return true;
        }

        private async Task<Boolean> IsHideoutMgmtLevelInRange(int _hideoutMgmtLevel)
        {
            if (_hideoutMgmtLevel <= 0)
            {
                HideoutMgmtLevel = 0;
                return true;
            }
            if (_hideoutMgmtLevel > 50)
            {
                HideoutMgmtLevel = 50;
                return true;
            }
            if (_hideoutMgmtLevel >= 1)
            {
                return true;
            }

            HideoutMgmtLevel = 0;
            return true;
        }


        #endregion

        #region Reset Functions
        protected async Task ResetCalculator()
        {
            ItemQuantity = 1;
            VendorBuyBack = 0;
            DesiredListPrice = 0;
            MarketPrice = 0;
            Investments = 0;
            BulkSale = false;
            BulkCheckBox = false;
            IntelDiscount = false;
            IntelCheckBox = false;
            CoreInputs = false;
            MarketPriceEntered = false;
            HideoutMgmtLevel = 0;

            _BasePrice = 0;
            _QuantityFactor = 1;
            _TotalBaseValue = 0;
            _BreakEvenPrice = 0;
            _UpperLimitPrice = 0;
            _LowestTax = 0;
            _MarginHigh = 0;
            _MarginLow = 0;

            _PO =  0;
            _PR =  0;
            _4PO = 0;
            _4PR = 0;

            _StandardTax = 0;
            _DiscountTax = 0;
            _DiscountPercentage = 30.0;
            _TotalTax = 0;
            _TaxPerItem = 0;
            _TaxPerItemDisplay = 0;
            _DiscountSavings = 0;
            _TaxPercentOfBase = 0;
            _PercentPaidInTax = 0;
            _EstSaleAmount = 0;
            _PerItemProfit_Gross = 0;
            _PerItemProfit_Realized = 0;
            _PerItemProfit_OverVendor = 0;
            _SaleGross = 0;
            _SaleNet = 0;
            _RealizedGains = 0;
            _FleaRepPercentGain = 0;
            _Valuation = 0;
            _LowestTax = 0;

            await UpdateGraphs();
        }

        private async Task ResetProStatsWhenZero()
        {
            _TotalBaseValue = 0;
            _BreakEvenPrice = 0;
            _UpperLimitPrice = 0;
            _LowestTax = 0;
            _MarginHigh = 0;
            _MarginLow = 0;

            await UpdateGraphs();
        }

        private async Task ResetTableStatsWhenZero()
        {
            _TotalTax = 0;
            _TaxPerItem = 0;
            _TaxPerItemDisplay = 0;
            _StandardTax = 0;
            _DiscountTax = 0;
            _DiscountSavings = 0;
            _TaxPercentOfBase = 0;
            _PercentPaidInTax = 0;
            _FleaRepPercentGain = 0;
            _SaleGross = 0;
            _SaleNet = 0;
            _RealizedGains = 0;
            _PerItemProfit_OverVendor = 0;
            _PerItemProfit_Gross = 0;            
            _PerItemProfit_GrossMinusTaxDisplay = 0;
            _PerItemProfit_Realized = 0;

            await UpdateGraphs();
        }
        
        #endregion

        #region Main Calculation

        //Formula:  VO x Ti x 4PO x Q + VR x Tr x 4PR x Q

        public async Task Calculate()
        {
            //Input Checks and Value Resets

            _VendorCoefficient = await SetVendorCoeff(VendorSelector);

            if(await IsItemQuantityLessThanOne(ItemQuantity))
            {
                await ResetProStatsWhenZero();
                await ResetTableStatsWhenZero();
            }            

            if(await IsVendorBuyBackLessThanOne(VendorBuyBack))
            {
                await ResetProStatsWhenZero();
                await ResetTableStatsWhenZero();
            }

            if(await IsDesiredListPriceLessThanOne(DesiredListPrice))
            {
                await ResetTableStatsWhenZero();
            }

            if(!await CheckMarketPriceInRange(MarketPrice))
            {               
                _Valuation = CalculateValuation(0, 0);
            }            

            if(await IsHideoutMgmtLevelInRange(HideoutMgmtLevel))
            {
                _DiscountPercentage = CalculateDiscountPercentage(HideoutMgmtLevel);
            }

            if(await IsInvestmentsInRange(Investments))
            {
                //Future Check for Investments Entered?
            }

            //Pre-Calculations

            if (await IsItemQuantityGreaterThanOrEqualToOne(ItemQuantity))
            {
                await UpdateGraphs();

                if (await IsVendorBuyBackGreaterThanZero(VendorBuyBack))
                {
                    _BasePrice = ConvertBaseValue(VendorBuyBack);                    
                    _QuantityFactor = CalculateQuantityFactor(BulkSale, ItemQuantity);
                    _TotalBaseValue = ConvertTotalBaseValue(_BasePrice, ItemQuantity, _QuantityFactor);
                    _BreakEvenPrice = CalculateBreakEvenPrice(BulkSale, IntelDiscount, VendorBuyBack, ItemQuantity, _VendorCoefficient);
                    _UpperLimitPrice = CalculateUpperLimit(BulkSale, IntelDiscount, _TotalBaseValue, ItemQuantity);
                    _LowestTax = CalculateLowestTax(_TotalBaseValue);
                    _MarginHigh = CalculateMarginsHigh(_TotalBaseValue, _MarkupCoeff, IntelDiscount);
                    _MarginLow = CalculateMarginsLow(_TotalBaseValue, _MarkdownCoeff, IntelDiscount);
                    await UpdateGraphs();

                    if (await IsDesiredListPriceGreaterThanZero(DesiredListPrice))
                    {
                        _PO = Calculate_PO(_TotalBaseValue, DesiredListPrice);
                        _PR = Calculate_PR(DesiredListPrice, _TotalBaseValue);
                        _4PO = Calculate_4PO(DesiredListPrice, _TotalBaseValue, _PO);
                        _4PR = Calculate_4PR(DesiredListPrice, _TotalBaseValue, _PR);
                        await UpdateGraphs();
                    }
                    else
                    {
                        await UpdateGraphs();
                        return;
                    }
                }
                else
                {
                    await UpdateGraphs();
                    return;
                }
            }
            else
            {
                await UpdateGraphs();
                return;
            }


            //Calculate Standard Outputs if Inputs Succeed
            _StandardTax = CalculateStandardTax(_TotalBaseValue, _4PO, _QuantityFactor, DesiredListPrice, _4PR);
            _DiscountTax = CalculateDiscountTax(_StandardTax);
            _TotalTax = CalculateTotalTax(IntelDiscount);
            _TaxPerItem = CalculateTaxPerItem(ItemQuantity, BulkSale, _TotalTax, _QuantityFactor);
            _TaxPerItemDisplay = TaxPerItemDisplay(_TaxPerItem);
            _DiscountSavings = CalculateDiscountSavings(IntelDiscount, _StandardTax, _DiscountTax);
            _TaxPercentOfBase = CalculateTaxPercentOfBase(_StandardTax, _TotalBaseValue, _QuantityFactor);
            _PercentPaidInTax = CalculatePercentPaidInTax(_TotalTax, DesiredListPrice, _QuantityFactor);
            _EstSaleAmount = CalculateEstSaleAmount(DesiredListPrice, _QuantityFactor);
            _PerItemProfit_Gross = CalculatePerItemProfit_Gross(ItemQuantity, DesiredListPrice, _TaxPerItem, BulkSale, _TotalTax);
            _PerItemProfit_Realized = CalculatePerItemProfit_Realized(_PerItemProfit_Gross, Investments, ItemQuantity);
            _PerItemProfit_OverVendor = CalculatePerItemProfit_OverVendor(ItemQuantity, DesiredListPrice, VendorBuyBack, _TotalTax, _QuantityFactor, BulkSale);
            _SaleGross = CalculateSaleGross(_EstSaleAmount);
            _SaleNet = CalculateSaleNet(_SaleGross, _TotalTax);
            _RealizedGains = CalculateRealizedGains(_SaleGross, _TotalTax, Investments);
            _FleaRepPercentGain = CalculateFleaRepPercentGain(_SaleNet);
            _Valuation = CalculateValuation(MarketPrice, _TotalBaseValue);
            _LowestTax = CalculateLowestTax(_TotalBaseValue);

            await UpdateGraphs();          

            return;
        }

        #endregion

        #region Calculation Functions
        //Calculation Functions

        private async Task<double> SetVendorCoeff(int _selectedVendor)
        {             
            return VendorCoeff[_selectedVendor];
        }
        private double ConvertBaseValue(int _vendorBuyBack)
        {
            if (_vendorBuyBack > 0)
            {
                return double.Parse((_vendorBuyBack / _VendorCoefficient).ToString("#"));
            }
            else
            {
                //Calculation Failed
                Console.WriteLine("Base Value Conversion Failed");
                return 0;
            }


            //ORIGINAL CODE - HARD CODED
            //if (_vendorBuyBack > 0)
            //{
            //    return double.Parse((_vendorBuyBack * _BaseValueCoeff).ToString("#"));
            //}
            //else
            //{
            //    return 0;
            //}
        }

        private double ConvertTotalBaseValue(double _basePrice, int _itemQuantity, int _quantityFactor)
        {
            return ((_basePrice * _itemQuantity) / _quantityFactor);
        }

        private int CalculateQuantityFactor(bool _bulkSale, int _itemQuantity)
        {
            if (_bulkSale)
            {
                return 1;
            }
            else
            {
                return _itemQuantity;
            }
        }

        private double VR_LessThan_VO(int _desiredListPrice, double _totalBaseValue)
        {
            if (_desiredListPrice < _totalBaseValue)
            {
                return _ExponentValue;
            }
            else
            {
                return 1;
            }
        }

        private double VR_GreaterThanOrEqual_VO(int _desiredListPrice, double _totalBaseValue)
        {
            if (DesiredListPrice >= _TotalBaseValue)
            {
                return _ExponentValue;
            }
            else
            {
                return 1;
            }
        }

        private double Calculate_PO(double _totalBaseValue, int _desiredListPrice)
        {
            double PO = (_totalBaseValue / _desiredListPrice);

            return Math.Log10(PO);
        }

        private double Calculate_PR(int _desiredListPrice, double _totalBaseValue)
        {
            double PR = (_desiredListPrice / _totalBaseValue);
            return Math.Log10(PR);
        }

        private double Calculate_4PO(int _desiredListPrice, double _totalBaseValue, double _po)
        {
            double ExpValue = VR_LessThan_VO(_desiredListPrice, _totalBaseValue);
            //Console.WriteLine("VR Less ? Exponent: " + ExpValue);
            double FourPO = Math.Pow(4, (Math.Pow(_po, ExpValue)));
            return FourPO;
        }

        private double Calculate_4PR(int _desiredListPrice, double _totalBaseValue, double _pr)
        {
            double ExpValue = VR_GreaterThanOrEqual_VO(_desiredListPrice, _totalBaseValue);
            //Console.WriteLine("VR Greater? Exponent: " + ExpValue);
            double FourPR = Math.Pow(4, (Math.Pow(_pr, ExpValue)));
            return FourPR;
        }

        private double CalculateStandardTax(double _totalBaseValue, double _4po, int _quantityFactor, int _desiredListPrice, double _4pr)
        {
			try
			{
                return double.Parse(((_totalBaseValue * _TaxConstantTi * _4po * _quantityFactor) + (_desiredListPrice * _TaxConstantTr * _4pr * _QuantityFactor)).ToString("#"));
            }
			catch
			{
                Console.WriteLine("Bug Standard Tax");
                return 0;
			}
        }

        private double CalculateDiscountTax(double _standardTax)
        {
			try
			{
                return double.Parse((_standardTax * (1 -_DiscountPercentage)).ToString("#"));
            }
			catch
			{
                Console.WriteLine("Bug Discount Tax");
                return 0;
			}
            
        }

        private double CalculateDiscountPercentage(double HideoutMgmtLevel)
        {
            HideoutMgmtLevel = (int)HideoutMgmtLevel;

            try
            {
                if (HideoutMgmtLevel <= 0)
                {
                    HideoutMgmtLevel = 0;
                    _DiscountPercentage = _DiscountBasePercentage;
                    return _DiscountPercentage;
                }

                if(HideoutMgmtLevel > 50)
                {
                    HideoutMgmtLevel = 50;
                    _DiscountPercentage = _DiscountBasePercentage * (1 + (HideoutMgmtLevel / 100));
                    return _DiscountPercentage;
                }

                if (HideoutMgmtLevel >= 1)
                {
                    _DiscountPercentage = _DiscountBasePercentage * (1 + (HideoutMgmtLevel / 100));
                    return _DiscountPercentage;
                }
            }
            catch
            {
                Console.WriteLine("Could not calculate Discount Percentage.");
                return _DiscountBasePercentage;
            }

            return _DiscountBasePercentage;            
        }

        private double CalculateTotalTax(bool _intelDiscount)
        {
            if (_intelDiscount)
            {
                return _DiscountTax;
            }
            else
            {
                return _StandardTax;
            }
        }

        private double CalculateTaxPerItem(int _itemQuantity, bool _bulkSale, double _totalTax, int _quantityFactor)
        {
			try
			{

                if (_itemQuantity == 1)
                {
                    if (_bulkSale)
                    {                        
                        return double.Parse((_totalTax / _itemQuantity).ToString("#"));
                    }
                    else
                    {                        
                        return double.Parse((_totalTax / _quantityFactor).ToString("#"));
                    }
                }
                else
                {
                    if (_bulkSale)
                    {                        
                        return double.Parse((_totalTax / _itemQuantity).ToString("#"));
                    }
                    else
                    {
                        return double.Parse((_totalTax / _quantityFactor).ToString("#"));
                    }
                }
            }
			catch
			{
                Console.WriteLine("Bug TaxPerItem");
                return 0;
			}
            
        }

        private double TaxPerItemDisplay(double _taxPerItem)
        {
            return _taxPerItem;
        }

        private double CalculateDiscountSavings(bool _intelDiscount, double _standardTax, double _discountTax)
        {
			try
			{
                if (_intelDiscount)
                {
                    return (_standardTax - _discountTax);
                }
                else
                {
                    return (_standardTax - _discountTax);
                }
            }
			catch
			{
                Console.WriteLine("Bug DiscountSavings");
                return 0;
            }
        }

        private double CalculateTaxPercentOfBase(double _standardTax, double _totalBaseValue, int _quantityFactor)
        {
			try
			{
                return double.Parse(((_standardTax / _totalBaseValue / _quantityFactor) * 100).ToString(".##"));
            }
			catch
			{
                Console.WriteLine("Bug TaxPercentOfBase");
                return 0;
			}
        }

        private double CalculatePercentPaidInTax(double _totalTax, int _desiredListPrice, int _quantityFactor)
        {
			try
			{
                return double.Parse(((_totalTax / _desiredListPrice / _quantityFactor) * 100).ToString(".##"));
            }
			catch
			{
                Console.WriteLine("Bug PercentPaidInTax");
                return 0;
			}
            
        }

        private double CalculateEstSaleAmount(int _desiredListPrice, int _quantityFactor)
        {
			try
			{
                return (_desiredListPrice * _quantityFactor);
			}
			catch
			{
                Console.WriteLine("Bug EstSaleAmount");
                return 0;
			}
        }

        private double CalculatePerItemProfit_Gross(int _itemQuantity, int _desiredListPrice, double _taxPerItem, bool _bulkSale, double _totalTax)
        {
			try
            {
                if (_itemQuantity == 1)
                {
                    return _desiredListPrice - _taxPerItem;
                }
                else
                {
                    if (_bulkSale)
                    {
                        return double.Parse(((_desiredListPrice / _itemQuantity) - (_totalTax / _itemQuantity)).ToString("#"));
                    }
                    else
                    {
                        return double.Parse((_desiredListPrice - _taxPerItem).ToString("#"));
                    }
                }
            }
			catch
			{
                Console.WriteLine("Bug PerItemProfitGross");
                return 0;
			}
        }

        private double CalculatePerItemProfit_Realized(double _perItemProfit_Gross, int _investments, int _itemQuantity)
        {
			try
			{
                return double.Parse((_perItemProfit_Gross - (_investments / _itemQuantity)).ToString("#"));          
			}
			catch
			{
                Console.WriteLine("Bug PerItemProfitRealized");
                return 0;
			}
        }

        private double CalculatePerItemProfit_OverVendor(int _itemQuantity, int _desiredListPrice, int _vendorBuyBack, double _totalTax, int _quantityFactor, bool _bulkSale)
        {
			try
			{
                if (_itemQuantity == 1)
                {
                    return double.Parse(((_desiredListPrice - _vendorBuyBack) - _totalTax / _quantityFactor).ToString("#"));
                }
                else
                {
                    if (_bulkSale)
                    {
                        return double.Parse(((_desiredListPrice / _itemQuantity - _vendorBuyBack) - (_totalTax / _itemQuantity)).ToString("#"));
                    }
                    else
                    {
                        return double.Parse(((_desiredListPrice - _vendorBuyBack) - _totalTax / _quantityFactor).ToString("#"));
                    }
                }
            }
			catch
			{
                Console.WriteLine("Bug PerItemProfitOverVendor");
                return 0;
			}

            
        }

        private double CalculateSaleGross(double _estSaleAmount)
        {
			try
			{
                return _estSaleAmount;
            }
			catch
			{
                Console.WriteLine("Bug CalcSalesGross");
                return 0;
			}
        }

        private  double CalculateSaleNet(double _saleGross, double _totalTax)
        {
			try
			{
                return _saleGross - _totalTax;
            }
			catch 
			{
                Console.WriteLine("Bug CalcSalesNet");
                return 0;				
			}
            
        }

        private double CalculateBreakEvenPrice(bool _bulkSale, bool _intelDiscount, int _vendorBuyBack, int _itemQuantity, double _vendorCoefficient)
        {
			try
			{
                if (_bulkSale)
                {
                    if (_intelDiscount)
                    {
                        return double.Parse(((_vendorBuyBack * _itemQuantity / VendorBreakEvenCoeff[VendorSelector] / (_BreakEvenDiscountCoeff + (0.00049799 * HideoutMgmtLevel))).ToString("#")));
                        //return double.Parse((_vendorBuyBack * _itemQuantity / (_BreakEvenDiscountCoeff + (0.00049799 * HideoutMgmtLevel))).ToString("#"));
                    }
                    else
                    {
                        return double.Parse(((_vendorBuyBack * _itemQuantity / VendorBreakEvenCoeff[VendorSelector])).ToString("#"));
                    }
                }
                else
                {
                    if (_intelDiscount)
                    {
                        double discountMult = (VendorDiscountIncrement[VendorSelector] * HideoutMgmtLevel);
                        return double.Parse((_vendorBuyBack * (VendorBreakEvenDiscountCoeff[VendorSelector] + discountMult) + _vendorBuyBack).ToString("#"));
                    }
                    else
                    {

                        return double.Parse(((_vendorBuyBack / VendorBreakEvenCoeff[VendorSelector])).ToString("#"));                        
                    }
                }
            }
			catch 
			{
				Console.WriteLine("Bug BreakEvenPrice");
                return 0;
			}

            
            
        }

        private double CalculateUpperLimit(bool _bulkSale, bool _intelDiscount, double _totalBaseValue, int _itemQuantity)
        {
            try
            {
                if (_bulkSale)
                {
                    if (_intelDiscount)
                    {
                        return double.Parse((_totalBaseValue * _itemQuantity * _UpperLimitCoeff).ToString("#"));
                    }
                    else
                    {
                        return double.Parse((_totalBaseValue * _itemQuantity * _UpperLimitCoeff).ToString("#"));
                    }
                }
                else
                {
                    if (_intelDiscount)
                    {
                        return double.Parse((_totalBaseValue * _UpperLimitCoeff).ToString("#"));
                    }
                    else
                    {
                        return double.Parse((_totalBaseValue * _UpperLimitCoeff).ToString("#"));
                    }
                }
            }
            catch
            {
                Console.WriteLine("Bug SummitPrice");
                return 0;
            }
        }


        private double CalculateMarginsHigh(double _totalBaseValue, double _markupCoeff, bool _intelDiscount)
        {
            try
            {
                if (_intelDiscount)
                {
                    return double.Parse((_totalBaseValue * _markupCoeff).ToString("#"));
                }
                else
                {
                    return double.Parse((_totalBaseValue * _markupCoeff).ToString("#"));
                }                    
            }
            catch
            {
				Console.WriteLine("Bug MarginsHigh");
                return 0;
            }
        }            

        private double CalculateMarginsLow(double _totalBaseValue, double _markdownCoeff, bool _intelDiscount)
        {
			try
			{
                if (_intelDiscount)
                {
                    return double.Parse((_totalBaseValue * _markdownCoeff).ToString("#"));
                }
                else
                {
                    return double.Parse((_totalBaseValue * _markdownCoeff).ToString("#"));
                }                    
            }
			catch
			{
				Console.WriteLine("Bug MarginsLow");
                return 0;
			}
        }

        private double CalculateRealizedGains(double _saleGross, double _totalTax, int _investments)
        {
			try
            {
                return (_saleGross - (_totalTax) - _investments);
            }
			catch (Exception)
			{
				Console.WriteLine("Bug RealizedGains");
                return 0;
			}
        }

        private double CalculateFleaRepPercentGain(double _saleNet)
        {
			try
			{
                return double.Parse(((_saleNet) / _FleaRepIncrement).ToString("##.00##"));
			}
			catch 
			{
				Console.WriteLine("Bug FleaPercentGain");
                return 0;
			}
        }

        private double CalculateValuation(int _marketPrice, double _totalBaseValue)
        {
			try
			{
                if (_marketPrice < 1)
                {
                    return 0.00;
                }
                else
                {
                    return double.Parse((_marketPrice / _totalBaseValue).ToString("#.00"));
                }
            }
			catch 
			{
				Console.WriteLine("Bug Valuation");
                return 0;
			}
        }

        private double CalculateLowestTax(double _totalBaseValue)
        {
			try
			{
                return double.Parse(((_totalBaseValue * _LowestTaxCoeff).ToString("#")));            
			}
			catch
			{
				Console.WriteLine("Bug LowestTax");
                return 0;
			}
        }

        #endregion

        #region Graph Functions
        //Graph Functions

        

        protected async Task UpdateGraphs()
        {
            await VendorGraph();
            await MarketGraph();
        }

        protected async Task ResetMarketGraph()
        {
            MarketGraphListPrice(false, 0);
            MarketGraphCalcProfitBreakEven(false, 0, 0);
            MarketGraphCalcProfitBaseValue(false, 0, 0);
            MarketGraphCalcValuation(false, 0, 0);
            MarketGraphCalcGainLoss(false, 0);
        }

        protected async Task ResetVendorGraph()
        {
            VendorGraphCalcBreakEven(0, 0, 0);
            VendorGraphCalcBreakEvenGains(false, 0);
            VendorGraphCalcBaseValue(false, 0, 0);
            VendorGraphCalcBaseValueGains(false, 0, 0, 0, false);
        }

        private async Task VendorGraph()
        {
            if(await IsItemQuantityGreaterThanOrEqualToOne(ItemQuantity))
            {
                if(await IsVendorBuyBackGreaterThanZero(VendorBuyBack))
                {
                    VendorGraphCalcBreakEven(ItemQuantity, VendorBuyBack, _BreakEvenPrice);
                }
                else
                {
                    await ResetVendorGraph();
                    return;
                } 

                if (await CheckCoreInputs(ItemQuantity, VendorBuyBack, DesiredListPrice))
                {
                    VendorGraphCalcBreakEvenGains(CoreInputs, _BreakEvenPrice);
                    VendorGraphCalcBaseValue(CoreInputs, VendorBuyBack, _TotalBaseValue);
                    VendorGraphCalcBaseValueGains(CoreInputs, VendorBuyBack, DesiredListPrice, _TotalBaseValue, IntelDiscount);
                }
                else
                {
                    VendorGraphCalcBreakEvenGains(false, _BreakEvenPrice);
                    VendorGraphCalcBaseValue(false, VendorBuyBack, _TotalBaseValue);
                    VendorGraphCalcBaseValueGains(false, VendorBuyBack, DesiredListPrice, _TotalBaseValue, IntelDiscount);
                    return;
                }
            }
            else
            {
                await ResetVendorGraph();
                return;
            }
        }
        private async Task MarketGraph()
        {            
            if(await IsItemQuantityGreaterThanOrEqualToOne(ItemQuantity))
            {
                if(await CheckMarketPriceInRange(MarketPrice))
                {
                    MarketGraphListPrice(MarketPriceEntered, MarketPrice);
                    MarketGraphCalcProfitBreakEven(MarketPriceEntered, VendorBuyBack, _BreakEvenPrice);
                    MarketGraphCalcProfitBaseValue(MarketPriceEntered, VendorBuyBack, _TotalBaseValue);
                    MarketGraphCalcValuation(MarketPriceEntered, VendorBuyBack, _TotalBaseValue);
                    MarketGraphCalcGainLoss(MarketPriceEntered, VendorBuyBack);
                }
                else
                {
                    await ResetMarketGraph();
                    return;
                }
            }
            else
            {
                await ResetMarketGraph();
                return;
            }
        }

        private void VendorGraphCalcBreakEven(int _itemQuantity, int _vendorBuyBack, double _breakEvenPrice)
        {
            if(_itemQuantity <= 0 || _vendorBuyBack <= 0 || _breakEvenPrice == 0)
            {
                VendorGraphValue[0] = 0;
                VendorGraphValue[1] = 0;
                VendorGraphValue[2] = 0;
                VendorGraphValue[3] = 0;
                VendorGraphValue[4] = 0;
                VendorGraphValue[5] = 0;
                VendorGraphValue[6] = 0;
            }
            else
            {
                VendorGraphValue[0] = double.Parse((_breakEvenPrice * 0.10 + _breakEvenPrice).ToString("#"));
                VendorGraphValue[1] = double.Parse((_breakEvenPrice * 0.15 + _breakEvenPrice).ToString("#"));
                VendorGraphValue[2] = double.Parse((_breakEvenPrice * 0.20 + _breakEvenPrice).ToString("#"));
                VendorGraphValue[3] = double.Parse((_breakEvenPrice * 0.25 + _breakEvenPrice).ToString("#"));
                VendorGraphValue[4] = double.Parse((_breakEvenPrice * 0.30 + _breakEvenPrice).ToString("#"));
                VendorGraphValue[5] = double.Parse((_breakEvenPrice * 0.35 + _breakEvenPrice).ToString("#"));
                VendorGraphValue[6] = double.Parse((_breakEvenPrice * 0.40 + _breakEvenPrice).ToString("#"));
            }            
        }

        private void VendorGraphCalcBreakEvenGains(bool _coreInputs, double _breakEvenPrice)
        {
            if (!_coreInputs || _breakEvenPrice <= 0)
            {
                VendorGraphValue[7]  = 0;
                VendorGraphValue[8]  = 0;
                VendorGraphValue[9]  = 0;
                VendorGraphValue[10] = 0;
                VendorGraphValue[11] = 0;
                VendorGraphValue[12] = 0;
                VendorGraphValue[13] = 0;
            }
            else
            {
				try
				{
                    VendorGraphValue[7] = double.Parse((_breakEvenPrice * 0.10).ToString("#"));
                    VendorGraphValue[8] = double.Parse((_breakEvenPrice * 0.15).ToString("#"));
                    VendorGraphValue[9] = double.Parse((_breakEvenPrice * 0.20).ToString("#"));
                    VendorGraphValue[10] = double.Parse((_breakEvenPrice * 0.25).ToString("#"));
                    VendorGraphValue[11] = double.Parse((_breakEvenPrice * 0.30).ToString("#"));
                    VendorGraphValue[12] = double.Parse((_breakEvenPrice * 0.35).ToString("#"));
                    VendorGraphValue[13] = double.Parse((_breakEvenPrice * 0.40).ToString("#"));
                }
				catch
				{
                    Console.WriteLine("Bugged VendorGraphCalcBreakEvenGains");
                    return;
				}
            }
        }

        private void VendorGraphCalcBaseValue(bool _coreInputs, int _vendorBuyBack, double _totalBaseValue)
        {
            if (!_coreInputs || _vendorBuyBack <= 0)
            {
                VendorGraphValue[14] = 0;
                VendorGraphValue[15] = 0;
                VendorGraphValue[16] = 0;
                VendorGraphValue[17] = 0;
                VendorGraphValue[18] = 0;
                VendorGraphValue[19] = 0;
                VendorGraphValue[20] = 0;
            }
            else
            {
				try
				{
                    VendorGraphValue[14] = double.Parse((_totalBaseValue * 0.10 + _totalBaseValue).ToString("#"));
                    VendorGraphValue[15] = double.Parse((_totalBaseValue * 0.15 + _totalBaseValue).ToString("#"));
                    VendorGraphValue[16] = double.Parse((_totalBaseValue * 0.20 + _totalBaseValue).ToString("#"));
                    VendorGraphValue[17] = double.Parse((_totalBaseValue * 0.25 + _totalBaseValue).ToString("#"));
                    VendorGraphValue[18] = double.Parse((_totalBaseValue * 0.30 + _totalBaseValue).ToString("#"));
                    VendorGraphValue[19] = double.Parse((_totalBaseValue * 0.35 + _totalBaseValue).ToString("#"));
                    VendorGraphValue[20] = double.Parse((_totalBaseValue * 0.40 + _totalBaseValue).ToString("#"));
                }
				catch
				{
                    Console.WriteLine("Bugged VendorGraphCalcBaseValue - ");
                    return;
				}
            }
        }

        private void VendorGraphCalcBaseValueGains(bool _coreInputs, int _vendorBuyBack, int _desiredListPrice, double _totalBaseValue, bool _intelDiscount)
        {
            if (!_coreInputs || _vendorBuyBack <= 0)
            {
                VendorGraphValue[21] = 0;
                VendorGraphValue[22] = 0;
                VendorGraphValue[23] = 0;
                VendorGraphValue[24] = 0;
                VendorGraphValue[25] = 0;
                VendorGraphValue[26] = 0;
                VendorGraphValue[27] = 0;
            }
            else
            {
                if(_vendorBuyBack <= 0 || _desiredListPrice <= 0)
				{
                    return;
				}
				else
				{
                    if (_intelDiscount)
                    {
						try
						{
                            VendorGraphValue[21] = double.Parse(((GraphTaxCalc((VendorGraphValue[14]) * 1.00, IntelDiscount, _DiscountBasePercentage)) - _totalBaseValue).ToString("#."));
                            VendorGraphValue[22] = double.Parse(((GraphTaxCalc((VendorGraphValue[15]) * 1.0013, IntelDiscount, _DiscountBasePercentage)) - _totalBaseValue).ToString("#."));
                            VendorGraphValue[23] = double.Parse(((GraphTaxCalc((VendorGraphValue[16]) * 1.00225, IntelDiscount, _DiscountBasePercentage)) - _totalBaseValue).ToString("#."));
                            VendorGraphValue[24] = double.Parse(((GraphTaxCalc((VendorGraphValue[17]) * 1.00310, IntelDiscount, _DiscountBasePercentage)) - _totalBaseValue).ToString("#."));
                            VendorGraphValue[25] = double.Parse(((GraphTaxCalc((VendorGraphValue[18]) * 1.0037, IntelDiscount, _DiscountBasePercentage)) - _totalBaseValue).ToString("#."));
                            VendorGraphValue[26] = double.Parse(((GraphTaxCalc((VendorGraphValue[19]) * 1.00430, IntelDiscount, _DiscountBasePercentage)) - _totalBaseValue).ToString("#."));
                            VendorGraphValue[27] = double.Parse(((GraphTaxCalc((VendorGraphValue[20]) * 1.00465, IntelDiscount, _DiscountBasePercentage)) - _totalBaseValue).ToString("#."));
                        }
						catch
						{
                            Console.WriteLine("Bugged VendorGraphCalcBaseValueGains w/ Discount...");
                            return;
						}
                    }
                    else
                    {
						try
						{
                            VendorGraphValue[21] = double.Parse(((GraphTaxCalc((VendorGraphValue[14]) * 1.00, IntelDiscount, _DiscountBasePercentage)) - _totalBaseValue).ToString("#."));
                            VendorGraphValue[22] = double.Parse(((GraphTaxCalc((VendorGraphValue[15]) * 1.002, IntelDiscount, _DiscountBasePercentage)) - _totalBaseValue).ToString("#."));
                            VendorGraphValue[23] = double.Parse(((GraphTaxCalc((VendorGraphValue[16]) * 1.0035, IntelDiscount, _DiscountBasePercentage)) - _totalBaseValue).ToString("#."));
                            VendorGraphValue[24] = double.Parse(((GraphTaxCalc((VendorGraphValue[17]) * 1.0048, IntelDiscount, _DiscountBasePercentage)) - _totalBaseValue).ToString("#."));
                            VendorGraphValue[25] = double.Parse(((GraphTaxCalc((VendorGraphValue[18]) * 1.005825, IntelDiscount, _DiscountBasePercentage)) - _totalBaseValue).ToString("#."));
                            VendorGraphValue[26] = double.Parse(((GraphTaxCalc((VendorGraphValue[19]) * 1.0066, IntelDiscount, _DiscountBasePercentage)) - _totalBaseValue).ToString("#."));
                            VendorGraphValue[27] = double.Parse(((GraphTaxCalc((VendorGraphValue[20]) * 1.00725, IntelDiscount, _DiscountBasePercentage)) - _totalBaseValue).ToString("#."));
                        }
						catch
						{
                            Console.WriteLine("Bugged VendorGraphCalcBaseValueGains w/o Discount...");
                            return;
                        }
                    }
                }
            }
        }

        private double GraphTaxCalc(double number, bool _intelDiscount, double _discountPercentage)
		{
            double taxCoeff = 0.09518716577540106951871657754011;
            double standardtax = number * taxCoeff;
            double discounttax = standardtax * _discountPercentage;
            if(_intelDiscount)
			{
                return number - discounttax;
			}
            else
			{
                return number - standardtax;
			}
		}



        private void MarketGraphListPrice(bool _marketPriceEntered, int _marketPrice)
        {
            if (_marketPriceEntered)
            {
                MarketGraphValue[0] = double.Parse((_marketPrice * 0.9255).ToString("#"));
                MarketGraphValue[1] = double.Parse((_marketPrice * 0.9500).ToString("#"));
                MarketGraphValue[2] = double.Parse((_marketPrice * 0.9750).ToString("#"));
                MarketGraphValue[3] = double.Parse((_marketPrice * 1.0000).ToString("#"));
                MarketGraphValue[4] = double.Parse((_marketPrice * 1.0500).ToString("#"));
                MarketGraphValue[5] = double.Parse((_marketPrice * 1.1000).ToString("#"));
                MarketGraphValue[6] = double.Parse((_marketPrice * 1.1500).ToString("#"));                
            }
            else
            {
                 MarketGraphValue[0] = 0;
                 MarketGraphValue[1] = 0;
                 MarketGraphValue[2] = 0;
                 MarketGraphValue[3] = 0;
                 MarketGraphValue[4] = 0;
                 MarketGraphValue[5] = 0;
                 MarketGraphValue[6] = 0;
            }
        }
        private void MarketGraphCalcProfitBreakEven(bool _marketPriceEntered, int _vendorBuyBack, double _breakEvenPrice)
        {
            if (_marketPriceEntered && _vendorBuyBack > 0)
            {
                MarketGraphValue[7]  = MarketGraphValue[0] - _breakEvenPrice;
                MarketGraphValue[8]  = MarketGraphValue[1] - _breakEvenPrice;
                MarketGraphValue[9]  = MarketGraphValue[2] - _breakEvenPrice;
                MarketGraphValue[10] = MarketGraphValue[3] - _breakEvenPrice;
                MarketGraphValue[11] = MarketGraphValue[4] - _breakEvenPrice;
                MarketGraphValue[12] = MarketGraphValue[5] - _breakEvenPrice;
                MarketGraphValue[13] = MarketGraphValue[6] - _breakEvenPrice;

            }
            else
            {
                 MarketGraphValue[7]  = 0;
                 MarketGraphValue[8]  = 0;
                 MarketGraphValue[9]  = 0;
                 MarketGraphValue[10] = 0;
                 MarketGraphValue[11] = 0;
                 MarketGraphValue[12] = 0;
                 MarketGraphValue[13] = 0;
            }
        }

        private void MarketGraphCalcProfitBaseValue(bool _marketPriceEntered, int _vendorBuyBack, double _totalBaseValue)
        {
            if (_marketPriceEntered && _vendorBuyBack > 0)
            {
                MarketGraphValue[14] = MarketGraphValue[0] - _totalBaseValue;
                MarketGraphValue[15] = MarketGraphValue[1] - _totalBaseValue;
                MarketGraphValue[16] = MarketGraphValue[2] - _totalBaseValue; 
                MarketGraphValue[17] = MarketGraphValue[3] - _totalBaseValue;
                MarketGraphValue[18] = MarketGraphValue[4] - _totalBaseValue;
                MarketGraphValue[19] = MarketGraphValue[5] - _totalBaseValue;
                MarketGraphValue[20] = MarketGraphValue[6] - _totalBaseValue;

            }
            else
            {
                MarketGraphValue[14] = 0;
                MarketGraphValue[15] = 0;
                MarketGraphValue[16] = 0;
                MarketGraphValue[17] = 0;
                MarketGraphValue[18] = 0;
                MarketGraphValue[19] = 0;
                MarketGraphValue[20] = 0;
            }
        }

        private void MarketGraphCalcValuation(bool _marketPriceEntered, int _vendorBuyBack, double _totalBaseValue)
        {
            if (_marketPriceEntered && _vendorBuyBack > 0)
            {
                MarketGraphValue[21] = double.Parse((MarketGraphValue[0] / _totalBaseValue).ToString("#.000"));
                MarketGraphValue[22] = double.Parse((MarketGraphValue[1] / _totalBaseValue).ToString("#.000"));
                MarketGraphValue[23] = double.Parse((MarketGraphValue[2] / _totalBaseValue).ToString("#.000"));
                MarketGraphValue[24] = double.Parse((MarketGraphValue[3] / _totalBaseValue).ToString("#.000"));
                MarketGraphValue[25] = double.Parse((MarketGraphValue[4] / _totalBaseValue).ToString("#.000"));
                MarketGraphValue[26] = double.Parse((MarketGraphValue[5] / _totalBaseValue).ToString("#.000"));
                MarketGraphValue[27] = double.Parse((MarketGraphValue[6] / _totalBaseValue).ToString("#.000"));
            }
            else
            {
                MarketGraphValue[21] = 0;
                MarketGraphValue[22] = 0;
                MarketGraphValue[23] = 0;
                MarketGraphValue[24] = 0;
                MarketGraphValue[25] = 0;
                MarketGraphValue[26] = 0;
                MarketGraphValue[27] = 0;

            }
        }

        private void MarketGraphCalcGainLoss(bool _marketPriceEntered, int _vendorBuyBack)
        {
            if (_marketPriceEntered && _vendorBuyBack > 0)
            {
                if(MarketGraphValue[7] < 1)
                {
                    MarketGraphLossGain[0] = "LOSS";
                }
                else
                {
                    MarketGraphLossGain[0] = "PROFIT";
                }

                if(MarketGraphValue[8] < 1)
                {
                    MarketGraphLossGain[1] = "LOSS";
                }
                else
                {
                    MarketGraphLossGain[1] = "PROFIT";
                }

                if (MarketGraphValue[9] < 1)
                {
                    MarketGraphLossGain[2] = "LOSS";
                }
                else
                {
                    MarketGraphLossGain[2] = "PROFIT";
                }

                if (MarketGraphValue[10] == 0)
                {
                    MarketGraphLossGain[3] = "EVEN";
                }
                else if (MarketGraphValue[10] < 1)
                {
                    MarketGraphLossGain[3] = "LOSS";
                }
                else
                {
                    MarketGraphLossGain[3] = "PROFIT";
                }

                if (MarketGraphValue[11] < 1)
                {
                    MarketGraphLossGain[4] = "LOSS";
                }
                else
                {
                    MarketGraphLossGain[4] = "PROFIT";
                }

                if (MarketGraphValue[12] < 1)
                {
                    MarketGraphLossGain[5] = "LOSS";
                }
                else
                {
                    MarketGraphLossGain[5] = "PROFIT";
                }

                if (MarketGraphValue[13] < 1)
                {
                    MarketGraphLossGain[6] = "LOSS";
                }
                else
                {
                    MarketGraphLossGain[6] = "PROFIT";
                }
            }
            else
            {
                MarketGraphLossGain[0] = "?";
                MarketGraphLossGain[1] = "?";
                MarketGraphLossGain[2] = "?";
                MarketGraphLossGain[3] = "?";
                MarketGraphLossGain[4] = "?";
                MarketGraphLossGain[5] = "?";
                MarketGraphLossGain[6] = "?";
            }
        }


        #endregion
    }
}
