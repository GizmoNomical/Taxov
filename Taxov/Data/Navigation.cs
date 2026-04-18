using System;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Taxov.Pages;
using Taxov.Pages.Calculator;
using Taxov.Pages.Charts;


namespace Taxov.Data
{
    public class Navigation : ComponentBase
    {        
        
        [Parameter]
        public Type _component { get; set; } = typeof(Freeloader);        

        public async Task RefreshPage()
        {
            StateHasChanged();
        }
        public async Task ServerOfflinePage()
        {          
            _component = typeof(ServerOffline);
            await RefreshPage();
        }

        //public async Task ProfilePage()
        //{
        //    _component = typeof(UserProfile);
        //    await RefreshPage();
        //}

        //public async Task AccessDenied() 
        //{
        //    _component = typeof(Denied);
        //    await RefreshPage();
        //}        
        
        public async Task HomePage()
        {
            _component = typeof(Freeloader);
            await RefreshPage();
        }

        //public async Task AboutPage()
        //{
        //    _component = typeof(About);
        //    await RefreshPage();
        //}

        public async Task ContactPage()
        {
            _component = typeof(Contact);
            await RefreshPage();
        }

        //public async Task FAQPage()
        //{
        //    _component = typeof(FAQPage);
        //    await RefreshPage();
        //}

        public async Task LegalPage()
        {
            _component = typeof(Legal);
            await RefreshPage();
        }

        //public async Task DevBlog()
        //{
        //    _component = typeof(DevBlog);
        //    await RefreshPage();
        //}

        //public async Task BoltActionPage()
        //{
        //    _component = typeof(BoltAction);
        //    await RefreshPage();
        //}
        //public async Task SemiAutoPage()
        //{
        //    _component = typeof(SemiAuto);
        //    await RefreshPage();
        //}
        //public async Task FullAutoPage()
        //{
        //    _component = typeof(FullAuto);
        //    await RefreshPage();
        //}
        //public async Task BeltFedPage()
        //{
        //    _component = typeof(BeltFed);
        //    await RefreshPage();
        //}
        
	}
}
