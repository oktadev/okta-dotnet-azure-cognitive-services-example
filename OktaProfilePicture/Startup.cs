using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Okta.AspNetCore;
using Okta.Sdk;
using Okta.Sdk.Configuration;

namespace OktaProfilePicture
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OktaDefaults.MvcAuthenticationScheme;
                })
                .AddCookie()
                .AddOktaMvc(new OktaMvcOptions
                {
                    OktaDomain = Configuration.GetValue<string>("Okta:Domain"),
                    ClientId = Configuration.GetValue<string>("Okta:ClientId"),
                    ClientSecret = Configuration.GetValue<string>("Okta:ClientSecret")
                });
            
            services.AddSingleton((serviceProvider) => new OktaClient(new OktaClientConfiguration()
            {
                OktaDomain = Configuration.GetValue<string>("Okta:Domain"),
                Token = Configuration.GetValue<string>("Okta:ApiToken")
            }));
            
            services.AddAzureClients(builder =>
            {
                builder.AddBlobServiceClient(Configuration.GetValue<string>("Azure:BlobStorageConnectionString"));
            });
            
            services.AddSingleton((serviceProvider) =>
                new FaceClient(
                    new ApiKeyServiceClientCredentials(Configuration.GetValue<string>("Azure:SubscriptionKey")))
                {
                    Endpoint = Configuration.GetValue<string>("Azure:FaceClientEndpoint")
                }
            );
            
            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
