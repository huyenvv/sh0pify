using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ProductListSwatchColorShopifyApp.Startup))]
namespace ProductListSwatchColorShopifyApp
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
