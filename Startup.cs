using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsuranceClaimManagement.Data;
using InsuranceClaimManagement.Models;
using InsuranceClaimManagement.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Http;
using System.Threading.RateLimiting;

namespace InsuranceClaimManagement
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
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:DefaultConnection must be configured with User Secrets or an environment variable.");
            }

            // Configure the DbContext to use MySQL
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySql(connectionString,
                new Microsoft.EntityFrameworkCore.MySqlServerVersion(new Version(8, 3, 0)))); // Use appropriate MySQL version

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.Password.RequiredLength = 12;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                    options.User.RequireUniqueEmail = true;
                    options.SignIn.RequireConfirmedEmail = true;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentOrganization, HttpCurrentOrganization>();
            services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();
            services.AddScoped<IAuditService, AuditService>();

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
            });

            services.Configure<SecurityStampValidatorOptions>(options =>
            {
                options.ValidationInterval = TimeSpan.FromMinutes(5);
            });

            services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromHours(2);
            });

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy("login", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 20,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        }));
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(PolicyNames.ClaimsRead,
                    policy => policy
                        .RequireRole(RoleNames.Administrator, RoleNames.ClaimsAdjuster, RoleNames.ReadOnly)
                        .RequireClaim(TenantClaimTypes.OrganizationId));
                options.AddPolicy(PolicyNames.ClaimsManage,
                    policy => policy
                        .RequireRole(RoleNames.Administrator, RoleNames.ClaimsAdjuster)
                        .RequireClaim(TenantClaimTypes.OrganizationId));
                options.AddPolicy(PolicyNames.CustomersManage,
                    policy => policy
                        .RequireRole(RoleNames.Administrator)
                        .RequireClaim(TenantClaimTypes.OrganizationId));
                options.AddPolicy(PolicyNames.StaffManage,
                    policy => policy
                        .RequireRole(RoleNames.Administrator)
                        .RequireClaim(TenantClaimTypes.OrganizationId));
            });

            services.AddRazorPages(options =>
            {
                options.Conventions.AuthorizePage("/Index", PolicyNames.ClaimsRead);
                options.Conventions.AuthorizeFolder("/Claims", PolicyNames.ClaimsRead);
                options.Conventions.AuthorizeFolder("/Users", PolicyNames.CustomersManage);
                options.Conventions.AuthorizeFolder("/Staff", PolicyNames.StaffManage);
                options.Conventions.AuthorizeFolder("/Audit", PolicyNames.StaffManage);
                options.Conventions.AllowAnonymousToPage("/Account/Login");
                options.Conventions.AllowAnonymousToPage("/Account/AccessDenied");
            });
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
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.Use(async (context, next) =>
            {
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["X-Frame-Options"] = "DENY";
                context.Response.Headers["Referrer-Policy"] = "no-referrer";
                await next();
            });

            app.UseRateLimiter();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
