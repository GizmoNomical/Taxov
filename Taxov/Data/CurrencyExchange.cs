using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Taxov.Data
{
    public class CurrencyExchange : ComponentBase
    {
        //User Inputs
        protected double Rus { get; set; } = 0;
        protected double Usd { get; set; } = 0;
        protected double Eur { get; set; } = 0;

        //Coefficients
        static readonly double RUStoUSD = 126;
        static readonly double RUStoEUR = 142;
        //static readonly double EURtoUSD = 1.1269841269841269841269841269841;
        //static readonly double USDtoEUR = 0.88732394366197183098591549295775;
        static readonly double EURtoUSD = RUStoEUR / RUStoUSD;
        static readonly double USDtoEUR = RUStoUSD / RUStoEUR;

        protected async Task ResetExchange()
        {
            Rus = 0;
            Usd = 0;
            Eur = 0;
        }

        //Async Task Runners
        protected async Task onClickCalcRUSAsync()
        {
            await CalcRUS();
        }
        protected async Task onClickCalcUSDAsync()
        {
            await CalcUSD();
        }
        protected async Task onClickCalcEURAsync()
        {
            await CalcEUR();
        }

        //Main Calc Functions
        protected async Task CalcRUS()
        {
            if (Rus == 0)
            {
                await ResetExchange();
                return;                
            }
            else
            {
                Usd = await RUS_To_USD(Rus);
                Eur = await RUS_To_Eur(Rus);
                return;
            }
        }

        protected async Task CalcUSD()
        {
            if (Usd == 0)
            {
                await ResetExchange();
                return;
            }

            Rus = await USD_To_RUS(Usd);
            Eur = await USD_To_EUR(Usd);            
            return;
        }
        protected async Task CalcEUR()
        {
            if (Eur == 0)
            {
                await ResetExchange();
                return;
            }

            Rus = await EUR_To_RUS(Eur);
            Usd = await EUR_To_USD(Eur);
            return;
        }

        //Secondary Functions

        protected async Task<double> RUS_To_USD(double _rus)
        {
            var rus_to_usd =  Utility.RoundToNearest((_rus / RUStoUSD), 0.01);

            if(rus_to_usd < 1)
            {
                rus_to_usd = 1;
            }

            return double.Parse(rus_to_usd.ToString("#"));
        }
        protected async Task<double> RUS_To_Eur(double _rus)
        {
            var rus_to_eur = Utility.RoundToNearest((_rus / RUStoEUR), 0.01);

            if(rus_to_eur < 1)
            {
                rus_to_eur = 1;
            }

            return double.Parse(rus_to_eur.ToString("#"));
        }
        protected async Task<double> USD_To_RUS(double _usd)
        {
            var usd_to_rus = Utility.RoundToNearest((_usd * RUStoUSD), 1);

            if(usd_to_rus < 1)
            {
                usd_to_rus = 1;
            }

            return double.Parse(usd_to_rus.ToString("#"));

        }
        protected async Task<double> USD_To_EUR(double _usd)
        {
            var usd_to_eur = Utility.RoundToNearest((_usd * USDtoEUR), 0.01);

            if(usd_to_eur < 1)
            {
                usd_to_eur = 1;
            }

            return double.Parse(usd_to_eur.ToString("#"));
        }
        protected async Task<double> EUR_To_RUS(double _eur)
        {
            var eur_to_rus = Utility.RoundToNearest((_eur * RUStoEUR), 1);

            if(eur_to_rus < 1)
            {
                eur_to_rus = 1;
            }

            return double.Parse(eur_to_rus.ToString("#"));
        }
        protected async Task<double> EUR_To_USD(double _eur)
        {
            var eur_to_usd = Utility.RoundToNearest((_eur * EURtoUSD), 0.01);

            if(eur_to_usd < 1)
            {
                eur_to_usd = 1;
            }

            return double.Parse(eur_to_usd.ToString("#"));
        }


    }
}