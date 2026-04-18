using System;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Taxov.Models
{
    public class TaxCalculator : ComponentBase
    {   
        //User Inputs
        protected int VendorBuyBack { get; set; } = 0;
        protected int DesiredListPrice { get; set; } = 0;
        protected int MarketPrice { get; set; } = 0;
        protected int Investments { get; set; } = 0;
        protected int ItemQuantity { get; set; } = 1;
        protected bool BulkSale { get; set; } = false;
        protected bool IntelDiscount { get; set; } = false;

        //Working Variables
        public double _BasePrice = 0;
        public double _TotalBaseValue = 0;
        public int _QuantityFactor = 1;
        public double _PO = 0;
        public double _PR = 0;
        public double _4PO = 0;
        public double _4PR = 0;

        //Coefficients
        private static readonly double _ExponentValue = 1.08;
        private static readonly double _TaxConstant = 0.05;
        private static readonly double _BaseValueCoeff = 1.5873015873015873015873015873016;
        private static readonly double _MarkupCoeff = 4.195;
        private static readonly double _MarkdownCoeff = 0.7452;
        private static readonly double _BaseBuyBackCoeff = 1.14093;
        private static readonly double _LowestTaxCoeff = 1.488372093;
        private static readonly double _DiscountPercentage = 0.67;
        private static readonly double _HighMarginPercent = 4.195;
        private static readonly double _LowMarginPercent = 0.7452;
        private static readonly double _FleaRepIncrement = 50000;

        //Output Variables
        public double _StandardTax = 000000000;
        public double _DiscountTax = 000000000;
        public double _TotalTax = 000000000;

        public double _TaxPerItem = 000000000;
        public double _DiscountSavings = 000000000;

        public double _TaxPercentOfBase = 0;
        public double _PercentPaidInTax = 0;

        public double _EstSaleAmount = 000000000;
        public double _SaleGross = 000000000;
        public double _SaleNet = 000000000;

        public double _FleaRepPercentGain = 0.0;
        public double _NetGains = 000000000;
        public double _Valuation = 0;

        public double _BreakEvenPrice = 000000000;
        public double _LowestTax = 000000000;

        public double _MarginHigh = 000000000;
        public double _MarginLow = 000000000;
        public double _PerItemProfit_OverVendor = 000000000;
        public double _PerItemProfit_Gross = 000000000;
        public double _PerItemProfit_Net = 000000000;


        //------------------------------------------------//
        //Main Tax Calculation
        //Formula:  VO x Ti x 4PO x Q + VR x Tr x 4PR x Q
        public async Task AutoCalculateAsync()
        {
            await Task.Run(() => Calculate());
        }
        protected async Task ToggleIntelDiscountAsync()
        {
            IntelDiscount = !IntelDiscount;
            await Task.Run(() => Calculate());
        }

        public void Calculate()
        {

            //Pre-Calculations
            if (VendorBuyBack > 0)
            {
                _BasePrice = ConvertBaseValue(VendorBuyBack);
                Console.WriteLine("Vendor BuyBack: " + VendorBuyBack);

                //Debug
                Console.WriteLine("Base Price: " + Utility.RoundToNearest(_BasePrice, 1));

                if (ItemQuantity >= 1)
                {
                    _QuantityFactor = CalculateQuantityFactor();
                    _TotalBaseValue = ConvertTotalBaseValue();

                    //Debug

                    Console.WriteLine("Total Base: " + _TotalBaseValue);

                    Console.WriteLine("Quantity Factor: " + _QuantityFactor);

                    if (DesiredListPrice > 0)
                    {
                        _PO = Calculate_PO();
                        _PR = Calculate_PR();
                        _4PO = Calculate_4PO();
                        _4PR = Calculate_4PR();

                        //Debug

                        Console.WriteLine("PO: " + _PO);

                        Console.WriteLine("PR: " + _PR);

                        Console.WriteLine("4PO: " + _4PO);

                        Console.WriteLine("4PR: " + _4PR);
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }



            //Calculate Standard Outputs
            _StandardTax = CalculateStandardTax();
            _DiscountTax = CalculateDiscountTax();
            _TotalTax = CalculateTotalTax();
            _TaxPerItem = CalculateTaxPerItem();
            _DiscountSavings = CalculateDiscountSavings();
            _TaxPercentOfBase = CalculateTaxPercentOfBase();
            _PercentPaidInTax = CalculatePercentPaidInTax();
            _EstSaleAmount = CalculateEstSaleAmount();
            _PerItemProfit_Gross = CalculatePerItemProfit_Gross();
            _PerItemProfit_Net = CalculatePerItemProfit_Net();
            _PerItemProfit_OverVendor = CalculatePerItemProfit_OverVendor();
            _SaleGross = CalculateSaleGross();
            _BreakEvenPrice = CalculateBreakEvenPrice();
            _MarginHigh = CalculateMarginsHigh();
            _MarginLow = CalculateMarginsLow();
            _NetGains = CalculateNetGains();
            _FleaRepPercentGain = CalculateFleaRepPercentGain();

            //Debug Logs



            return;
        }

        //---------------------//
        //Calculation Functions
        private static double ConvertBaseValue(int number)
        {
            if (number > 0)
            {
                return double.Parse((number * _BaseValueCoeff).ToString("#"));
            }
            else
            {
                return 0;
            }
        }

        private double ConvertTotalBaseValue()
        {
            return ((_BasePrice * ItemQuantity) / _QuantityFactor);
        }

        private int CalculateQuantityFactor()
        {
            if (BulkSale)
            {
                return 1;
            }
            else
            {
                return ItemQuantity;
            }
        }

        private double VR_LessThan_VO()
        {
            if (DesiredListPrice < _TotalBaseValue)
            {
                return _ExponentValue;
            }
            else
            {
                return 1;
            }
        }

        private double VR_GreaterThanOrEqual_VO()
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

        private double Calculate_PO()
        {
            double PO = (_TotalBaseValue / DesiredListPrice);

            return Math.Log10(PO);
        }

        private double Calculate_PR()
        {
            double PR = (DesiredListPrice / _TotalBaseValue);
            return Math.Log10(PR);
        }

        private double Calculate_4PO()
        {
            double ExpValue = VR_LessThan_VO();
            Console.WriteLine("VR Less ? Exponent: " + ExpValue);
            double FourPO = Math.Pow(4, (Math.Pow(_PO, ExpValue)));
            return FourPO;
        }

        private double Calculate_4PR()
        {
            double ExpValue = VR_GreaterThanOrEqual_VO();
            Console.WriteLine("VR Greater? Exponent: " + ExpValue);
            double FourPR = Math.Pow(4, (Math.Pow(_PR, ExpValue)));
            return FourPR;
        }

        private double CalculateStandardTax()
        {
            return double.Parse(((_TotalBaseValue * _TaxConstant * _4PO * _QuantityFactor) + (DesiredListPrice * _TaxConstant * _4PR * _QuantityFactor)).ToString("#"));
        }

        private double CalculateDiscountTax()
        {
            return double.Parse((_StandardTax * _DiscountPercentage).ToString("#"));
        }

        private double CalculateTotalTax()
        {
            if (IntelDiscount)
            {
                return _DiscountTax;
            }
            else
            {
                return _StandardTax;
            }
        }

        private double CalculateTaxPerItem()
        {
            return (_StandardTax / _QuantityFactor);
        }

        private double CalculateDiscountSavings()
        {
            if (IntelDiscount)
            {
                return (_StandardTax - _DiscountTax);
            }
            else
            {
                return 0;
            }
        }

        private double CalculateTaxPercentOfBase()
        {
            return (_StandardTax / _TotalBaseValue / _QuantityFactor);
        }

        private double CalculatePercentPaidInTax()
        {
            return (_TotalTax / DesiredListPrice / _QuantityFactor);
        }

        private double CalculateEstSaleAmount()
        {
            return (DesiredListPrice * ItemQuantity);
        }

        private double CalculatePerItemProfit_Gross()
        {
            return ((DesiredListPrice / _QuantityFactor) - _TaxPerItem);
        }

        private double CalculatePerItemProfit_Net()
        {
            return _PerItemProfit_Gross - (Investments / _QuantityFactor);
        }

        private double CalculatePerItemProfit_OverVendor()
        {
            return (DesiredListPrice - VendorBuyBack) - _TotalTax / _QuantityFactor;
        }

        private double CalculateSaleGross()
        {
            return _PerItemProfit_Gross * ItemQuantity;
        }

        private double CalculateBreakEvenPrice()
        {
            return (VendorBuyBack * _BaseBuyBackCoeff);
        }

        private double CalculateMarginsHigh()
        {
            return _TotalBaseValue * _MarkupCoeff;
        }

        private double CalculateMarginsLow()
        {
            return _TotalBaseValue * _MarkdownCoeff;
        }

        private double CalculateNetGains()
        {
            return _SaleGross - Investments;
        }

        private double CalculateFleaRepPercentGain()
        {
            return double.Parse((_SaleGross / _FleaRepIncrement / 100).ToString("#.0000"));
        }
    }
}
