using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Taxov.Models
{
    public class CurrencyExchange : ComponentBase
    {
        protected double Rus { get; set; } = 0;
        protected double Usd { get; set; } = 0;
        protected double Eur { get; set; } = 0;

        static readonly double RUStoUSD = 125;
        static readonly double RUStoEUR = 145;
        static readonly double EURtoUSD = 1.16;
        static readonly double USDtoEUR = 0.0827586206896552;

        protected async Task onClickCalcRUSAsync()
        {
            await Task.Run(() => CalcRUS());
        }
        protected async Task onClickCalcUSDAsync()
        {
            await Task.Run(() => CalcUSD());
        }
        protected async Task onClickCalcEURAsync()
        {
            await Task.Run(() => CalcEUR());
        }

        protected void CalcRUS()
        {
            if (Rus == 0)
            {
                Rus = 0;
                Usd = 0;
                Eur = 0;
                return;
            }

            Usd = 0;
            Eur = 0;
            Usd = Utility.RoundToNearest((Rus / RUStoUSD), 0.01);
            Eur = Utility.RoundToNearest((Rus / RUStoEUR), 0.01);
            return;
        }

        protected void CalcUSD()
        {
            if (Usd == 0)
            {
                Rus = 0;
                Usd = 0;
                Eur = 0;
                return;
            }

            Rus = 0;
            Eur = 0;
            Rus = Utility.RoundToNearest((Usd * RUStoUSD), 1);
            Eur = Utility.RoundToNearest((Usd * USDtoEUR), 0.01);
        }

        protected void CalcEUR()
        {
            if (Eur == 0)
            {
                Rus = 0;
                Usd = 0;
                Eur = 0;
                return;
            }

            Rus = 0;
            Usd = 0;
            Rus = Utility.RoundToNearest((Eur * RUStoEUR), 1);
            Usd = Utility.RoundToNearest((Eur * EURtoUSD), 0.01);
        }
    }
}