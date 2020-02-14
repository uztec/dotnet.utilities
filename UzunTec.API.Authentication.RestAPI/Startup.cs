﻿using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using SimpleInjector;
using SimpleInjector.Integration.ServiceCollection;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using UzunTec.API.Authentication.RestAPI.Authentication;

namespace UzunTec.API.Authentication.RestAPI
{
    public class Startup : IDisposable
    {
        private readonly APIAuthContainer container;
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
            this.container = new APIAuthContainer(configuration);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()//x => x.Filters.Add(new AuthorizeFilter("Bearer")))
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
            .AddJsonOptions(delegate (MvcJsonOptions options)
            {
                IContractResolver resolver = options.SerializerSettings.ContractResolver;
                if (resolver != null)
                {
                    DefaultContractResolver res = resolver as DefaultContractResolver;
                    res.NamingStrategy = new CamelCaseNamingStrategy(false, true);
                }
                options.SerializerSettings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy(true, true)));
                options.SerializerSettings.DateFormatString = "yyyy-MM-dd";
                options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            });

            #region Swagger

            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(delegate (SwaggerGenOptions options)
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "UzunTec API Authorization Tools", Version = "v1" });
                options.DescribeAllParametersInCamelCase();
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter into field the word 'Bearer' following by space and JWT",
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header,
                        },new List<string>()
                    }
                });
            });

            #endregion

            #region Simple Injector
            services.AddSimpleInjector(this.container, delegate (SimpleInjectorAddOptions options)
            {
                options.AddAspNetCore().AddControllerActivation().AddViewComponentActivation();
            });
            #endregion

            #region Authentication

            AuthenticationConfig authenticationConfig = this.Configuration.GetSection("AuthSettings").Get<AuthenticationConfig>();
            Authenticator authenticator = new Authenticator(authenticationConfig);
            services.AddSingleton<AuthenticationConfig>(authenticationConfig);
            services.AddSingleton<Authenticator>(authenticator);

            services.AddAuthentication(delegate (AuthenticationOptions authOptions)
            {
                authOptions.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                authOptions.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(authenticator.SetBearerTokenOptions);

            #endregion

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseSimpleInjector(this.container);
            this.container.Initialize();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseMvc();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(delegate (SwaggerUIOptions options)
            {
                options.SwaggerEndpoint("./v1/swagger.json", "UzunTec API Authorization Tools");
                options.RoutePrefix = "swagger";
            });

            // global cors policy
            app.UseCors(delegate (CorsPolicyBuilder builder) { builder.AllowAnyMethod().AllowAnyOrigin().AllowAnyHeader(); });
        }

        public void Dispose()
        {
            this.container.Dispose();
        }
    }
}