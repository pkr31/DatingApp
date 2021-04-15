using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace DatingApp.API {
  public class Startup {
    public Startup (IConfiguration configuration) {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureDevelopmentServices(IServiceCollection services)
        {
            services.AddDbContext<DataContext>(x => {
               // x.UseLazyLoadingProxies();
                x.UseSqlite(Configuration.GetConnectionString("DefaultConnection"));
            });

            ConfigureServices(services);
        }

        public void ConfigureProductionServices(IServiceCollection services)
        {
            //services.AddDbContext<DataContext>(x => {
            //    x.UseLazyLoadingProxies();
            //    x.UseMySql(Configuration.GetConnectionString("DefaultConnection"));
            //});

            //ConfigureServices(services);
        }
        public void ConfigureServices (IServiceCollection services) {
      IdentityBuilder builder = services.AddIdentityCore<User> (opt => {
        opt.Password.RequireDigit = false;
        opt.Password.RequiredLength = 4;
        opt.Password.RequireNonAlphanumeric = false;
        opt.Password.RequireUppercase = false;
      });

      builder = new IdentityBuilder (builder.UserType, typeof (Role), builder.Services);
      builder.AddEntityFrameworkStores<DataContext> ();
      builder.AddRoleValidator<RoleValidator<Role>> ();
      builder.AddRoleManager<RoleManager<Role>> ();
      builder.AddSignInManager<SignInManager<User>> ();
      services.AddAuthentication (JwtBearerDefaults.AuthenticationScheme)
        .AddCookie (cfg => cfg.SlidingExpiration = true)
        .AddJwtBearer (options => {
          options.TokenValidationParameters = new TokenValidationParameters {
          ValidateIssuerSigningKey = true,
          IssuerSigningKey = new SymmetricSecurityKey (Encoding.ASCII
          .GetBytes (Configuration.GetSection ("AppSettings:Token").Value)),
          ValidateIssuer = false,
          ValidateAudience = false
          };
        });
          //  services.AddAuthorization();
      //services.AddAuthorization(options =>
      //{
      //    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
      //    options.AddPolicy("ModeratePhotoRole", policy => policy.RequireRole("Admin", "Moderator"));
      //    options.AddPolicy("VipOnly", policy => policy.RequireRole("VIP"));
      //});

            services.AddControllers (options => {
        var policy = new AuthorizationPolicyBuilder ()
          .RequireAuthenticatedUser ()
          .Build ();
        options.Filters.Add (new AuthorizeFilter (policy));
      });
      //.SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
      //.AddJsonOptions(opt =>
      //{
      //    opt.SerializerSettings.ReferenceLoopHandling =
      //        Newtonsoft.Json.ReferenceLoopHandling.Ignore;
      //});

      services.AddSwaggerGen (c => {
        c.SwaggerDoc ("v1", new OpenApiInfo {
          Title = "Dating App API",
            Version = "v1"
        });
        c.AddSecurityDefinition ("Bearer", new OpenApiSecurityScheme {
          In = ParameterLocation.Header,
            Description = "Please insert JWT with Bearer into field",
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey
        });
        c.AddSecurityRequirement (new OpenApiSecurityRequirement {
          {
            new OpenApiSecurityScheme {
              Reference = new OpenApiReference {
                Type = ReferenceType.SecurityScheme,
                  Id = "Bearer"
              }
            },
            new string[] { }
          }
        });
      });
      services.AddMvc ().SetCompatibilityVersion (CompatibilityVersion.Version_3_0);
      services.AddDbContext<DataContext> (x => x.UseSqlite (Configuration.GetConnectionString ("DefaultConnection")));
      services.AddCors (options => {
        options.AddPolicy ("CorsPolicy",
          builder => builder.AllowAnyOrigin ()
          .AllowAnyMethod ()
          .AllowAnyHeader ());
      });
      services.Configure<CloudinarySettings> (Configuration.GetSection ("CloudinarySettings"));
      services.AddAutoMapper (typeof (DatingRepository).Assembly);
      //services.AddScoped<IAuthRepository, AuthRepository>();
      services.AddScoped<IDatingRepository, DatingRepository> ();

      services.AddScoped<LogUserActivity> ();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure (IApplicationBuilder app, IWebHostEnvironment env) {
      if (env.IsDevelopment ()) {
        app.UseDeveloperExceptionPage ();
      } else {
        app.UseExceptionHandler (builder => {
          builder.Run (async context => {
            context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;

            var error = context.Features.Get<IExceptionHandlerFeature> ();
            if (error != null) {
              context.Response.AddApplicationError (error.Error.Message);
              await context.Response.WriteAsync (error.Error.Message);
            }
          });
        });
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        // app.UseHsts();
      }

      //  app.UseHttpsRedirection();
      app.UseSwagger ();

      // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
      // specifying the Swagger JSON endpoint.
      app.UseSwaggerUI (c => {
        c.SwaggerEndpoint ("/swagger/v1/swagger.json", "My API V1");
        c.RoutePrefix = string.Empty;
      });
      //  app.UseHttpsRedirection();
      app.UseRouting ();
      app.UseCors ("CorsPolicy");
      app.UseAuthentication ();
      app.UseAuthorization ();
      app.UseEndpoints (endpoints => {
        endpoints.MapControllers ().RequireCors ("CorsPolicy");
      });

      //   app.UseCors();
      //   app.UseRouting();

      //   app.UseAuthentication();
      //   app.UseAuthorization();

      //   app.UseDefaultFiles();
      //   app.UseStaticFiles();
      //   app.UseSwagger();

      //   // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
      //   // specifying the Swagger JSON endpoint.
      //   app.UseSwaggerUI(c =>
      //   {
      //     c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
      //     c.RoutePrefix = string.Empty;
      //   });
      //   app.UseEndpoints(endpoints =>
      //   {
      //     endpoints.MapControllers();
      //     // endpoints.MapFallbackToController("Index", "Fallback");
      //   });
    }
  }
}