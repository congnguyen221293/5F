﻿// TCDev.de 2022/03/16
// TCDev.APIGenerator.ApiGeneratorExtension.cs
// https://github.com/DeeJayTC/net-dynamic-api

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using EFCore.AutomaticMigrations;
using EntityFramework.Triggers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.SwaggerUI;
using TCDev.ApiGenerator.Attributes;
using TCDev.ApiGenerator.Data;
using TCDev.APIGenerator.Extension;
using TCDev.APIGenerator.Extension.Swagger;
using TCDev.ApiGenerator.Json;
using TCDev.APIGenerator.Schema;
using TCDev.APIGenerator.Services;
using TCDev.Controllers;
using TCDev.APIGenerator.Data;

namespace TCDev.ApiGenerator.Extension
{
    public static class ApiGeneratorExtension
    {
        public static ApiGeneratorConfig ApiGeneratorConfig { get; set; } = new(null);

        public static async Task<IServiceCollection> AddApiGeneratorServices(
            this IServiceCollection services,
            IConfiguration config,
            Assembly assembly)
        {
            ApiGeneratorConfig = new ApiGeneratorConfig(config);

            // APIGen Services
            AddDataContext(services, config, assembly);
            AddAssemblyHandling(services, assembly);
            AddSwagger(services);

            services
                .AddHttpContextAccessor()
                .AddSingleton(typeof(ITriggers<,>), typeof(Triggers<,>))
                .AddSingleton(typeof(ITriggers<>),  typeof(Triggers<>))
                .AddSingleton(typeof(ITriggers),    typeof(Triggers))
                .AddScoped(typeof(ApplicationDataService))
                .AddScoped(typeof(ODataScopeService<,>))
                .AddScoped(typeof(IGenericRespository<,>), typeof(GenericRespository<,>));
            return services;
        }

        private static void AddOdataServices(IServiceCollection services, AssemblyService assemblyService)
        {
            if (ApiGeneratorConfig.ODataOptions.Enabled)
            {
                services
                    .AddControllers()
                    .AddOData(opt =>
                    {
                        opt.AddRouteComponents("odata", GenericDbContext.GetEdmModel(assemblyService));
                        opt.EnableNoDollarQueryOptions = true;
                        opt.EnableQueryFeatures(20000);
                        opt.Select()
                            .Expand()
                            .OrderBy()
                            .SetMaxTop(10000)
                            .Count()
                            .SkipToken()
                            .Filter();
                    })
                    .AddJsonOptions(o => { o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); }
                    );
            }
            else
            {
                services
                    .AddControllers()
                    .AddJsonOptions(o => { o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });
            }
        }

        private static void AddDataContext(IServiceCollection services, IConfiguration config, Assembly assembly)
        {
            switch (ApiGeneratorConfig.DatabaseOptions.DatabaseType)
            {
                case DbType.InMemory:

                    if (ApiGeneratorConfig.IdentityOptions.EnableIdentity)
                    {
                        services.AddDbContext<AuthDbContext>(options =>
                            options.UseInMemoryDatabase("ApiGeneratorAuth"));
                    }
                    else
                    {
                        services.AddDbContext<AuthDbContext>();
                    }

                    services.AddDbContext<GenericDbContext>(options =>
                        options.UseInMemoryDatabase("ApiGeneratorDatabase"));
                    break;
                case DbType.Sql:
                    if (ApiGeneratorConfig.IdentityOptions.EnableIdentity)
                    {
                        services.AddDbContext<AuthDbContext>(options =>
                        options.UseSqlServer(config.GetConnectionString("ApiGeneratorAuth"),
                            b => b.MigrationsAssembly(assembly.FullName)));
                    }
                    else
                    {
                        services.AddDbContext<AuthDbContext>();
                    }

                    services.AddDbContext<GenericDbContext>(options =>
                        options.UseSqlServer(config.GetConnectionString("ApiGeneratorDatabase"),
                            b => b.MigrationsAssembly(assembly.FullName)));
                    break;


                case DbType.SqLite:

                    if (ApiGeneratorConfig.IdentityOptions.EnableIdentity)
                    {
                        services.AddDbContext<AuthDbContext>(options =>
                        options.UseSqlite(config.GetConnectionString("ApiGeneratorAuth"),
                            b => b.MigrationsAssembly(assembly.FullName)));
                    }
                    else
                    {
                        services.AddDbContext<AuthDbContext>();
                    }


                    services.AddDbContext<GenericDbContext>(options =>
                        options.UseSqlite(config.GetConnectionString("ApiGeneratorDatabase"),
                            b => b.MigrationsAssembly(assembly.FullName)));


                    break;

                case DbType.Postgres:

                    if (ApiGeneratorConfig.IdentityOptions.EnableIdentity)
                    {
                        services.AddDbContext<AuthDbContext>(options =>
                        options.UseNpgsql(config.GetConnectionString("ApiGeneratorAuth"),
                            b => b.MigrationsAssembly(assembly.FullName)));
                    }
                    else
                    {
                        services.AddDbContext<AuthDbContext>();
                    }


                    services.AddDbContext<GenericDbContext>(options =>
                        options.UseNpgsql(config.GetConnectionString("ApiGeneratorDatabase"),
                            b => b.MigrationsAssembly(assembly.FullName)));
                    break;
                default:
                    throw new Exception("Database Type Unkown");
            }
        }

        private static void AddSwagger(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(ApiGeneratorConfig.SwaggerOptions.Version,
                    new OpenApiInfo
                    {
                        Title = ApiGeneratorConfig.SwaggerOptions.Title,
                        Version = ApiGeneratorConfig.SwaggerOptions.Version,
                        Description = ApiGeneratorConfig.SwaggerOptions.Description,
                        Contact = new OpenApiContact
                        {
                            Email = ApiGeneratorConfig.SwaggerOptions.ContactMail,
                            Url = new Uri(ApiGeneratorConfig.SwaggerOptions.ContactUri)
                        }
                    });
                c.DocumentFilter<ShowInSwaggerFilter>();
                c.SchemaFilter<SwaggerSchemaFilter>();
                c.OperationFilter<IgnoreODataQueryOptionOperationFilter>();

                if (ApiGeneratorConfig.ODataOptions.Enabled)
                {
                    c.OperationFilter<EnableQueryFiler>();
                }

                if (ApiGeneratorConfig.ApiOptions.UseXmlComments)
                {
                    if (!string.IsNullOrEmpty(ApiGeneratorConfig.ApiOptions.XmlCommentsFile))
                    {
                        throw new Exception("You need to set XMLCommentsFile option when using XMl Comments");
                    }

                    c.IncludeXmlComments(ApiGeneratorConfig.ApiOptions.XmlCommentsFile, true);
                }
            });
        }

        private static void AddAssemblyHandling(IServiceCollection services, Assembly assembly)
        {
            var assemblyService = new AssemblyService();
            services.AddSingleton(assemblyService);

            switch (ApiGeneratorConfig.ApiOptions.JsonMode)
            {
                case "local":

                    try
                    {
                        if(File.Exists(ApiGeneratorConfig.ApiOptions.JsonUri)) {
                        var jsonDefsLocal = JsonConvert.DeserializeObject<List<JsonClassDefinition>>(
                            File.ReadAllText(ApiGeneratorConfig.ApiOptions.JsonUri));
                        assemblyService.Types.AddRange(JsonClassBuilder.CreateTypes(jsonDefsLocal));
                        }
                    }
                    catch (FileNotFoundException ex)
                    {
                        throw new Exception($"Json Definition File not found, make sure its stored in {ApiGeneratorConfig.ApiOptions.JsonUri}", ex);
                    }
                    break;
                case "remote":

                    using (var client = new HttpClient())
                    {
                        // This is a blocking call, yes, on purpose!
                        var jsonDefRaw = client.GetStringAsync(ApiGeneratorConfig.ApiOptions.JsonUri).Result;
                        var jsonDefRemote = JsonConvert.DeserializeObject<List<JsonClassDefinition>>(jsonDefRaw);
                        assemblyService.Types.AddRange(JsonClassBuilder.CreateTypes(jsonDefRemote));
                    }

                    break;
                default:
                    throw new Exception("JsonMode not supported, has to be local or remote");
            }



            assemblyService.Types.AddRange(assembly.GetExportedTypes()
                .Where(x => x.GetCustomAttributes<ApiAttribute>()
                    .Any()));




            // Put everything together
            services.AddMvc(options =>
                    options.Conventions.Add(new GenericControllerRouteConvention()))
                .ConfigureApplicationPartManager(manager =>  manager.FeatureProviders.Add(new GenericTypeControllerFeatureProvider(assemblyService.Types))
                );


            AddOdataServices(services, assemblyService);
        }

        public static IApplicationBuilder UseAutomaticApiMigrations(this IApplicationBuilder app, bool allowDataLoss = false)
        {
            using var serviceScope = app.ApplicationServices.CreateScope();
            var dbContext = serviceScope.ServiceProvider.GetService<GenericDbContext>();
            if (ApiGeneratorConfig.DatabaseOptions.DatabaseType != DbType.InMemory)
            {
                dbContext.MigrateToLatestVersion(new DbMigrationsOptions
                {
                    AutomaticMigrationsEnabled = true, 
                    AutomaticMigrationDataLossAllowed = allowDataLoss
                });
            }

            return app;
        }

        public static IApplicationBuilder UseApiGenerator(this IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                //c.InjectStylesheet("/SwaggerDarkTheme.css");
                c.OAuthConfigObject = new OAuthConfigObject()
                {
                    AppName = "ApiGenerator",
                    ClientId = string.Empty,
                    ClientSecret = string.Empty,
                    
                };
                c.SwaggerEndpoint(
                    "/swagger/v1/swagger.json",
                    $"{ApiGeneratorConfig.SwaggerOptions.Title} {ApiGeneratorConfig.SwaggerOptions.Version}"
                );
            });

            return app;
        }

        public static IEndpointRouteBuilder UseApiGeneratorEndpoints(this IEndpointRouteBuilder builder)
        {
            return builder;
        }
    }
}
