using FluentMigrator.Runner;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Npgsql;
using OrderService.Infrastructure.EventBus;
using OrderService.Infrastructure.Persistence;
using OrderService.UseCases.Commands;
using OrderService.UseCases.Queries;
using System.Data;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrderService.Http
{
    public class Startup(IConfiguration configuration)
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.DefaultIgnoreCondition =
                        JsonIgnoreCondition.WhenWritingNull;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            var keycloakAuthority = configuration["Keycloak:Authority"];
            var audience = configuration["Keycloak:Audience"];

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = keycloakAuthority;
                    options.Audience = audience;

                    options.RequireHttpsMetadata = false;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = keycloakAuthority,

                        ValidateAudience = true,
                        ValidAudience = audience,

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1),
                        RoleClaimType = "roles"
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = context =>
                        {
                            var identity = context.Principal?.Identity as ClaimsIdentity;
                            if (identity == null) return Task.CompletedTask;

                            var realmAccessClaim = context.Principal?.Claims
                                .FirstOrDefault(c => c.Type == "realm_access")?.Value;

                            if (!string.IsNullOrEmpty(realmAccessClaim))
                            {
                                try
                                {
                                    using var doc = JsonDocument.Parse(realmAccessClaim);
                                    if (doc.RootElement.TryGetProperty("roles", out var rolesElement))
                                    {
                                        foreach (var role in rolesElement.EnumerateArray())
                                        {
                                            var roleName = role.GetString();
                                            if (!string.IsNullOrEmpty(roleName))
                                            {
                                                identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                                                identity.AddClaim(new Claim("roles", roleName));
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Failed to parse realm_access: {ex.Message}");
                                }
                            }

                            return Task.CompletedTask;
                        },

                        OnAuthenticationFailed = context =>
                        {
                            Console.WriteLine($"[AUTH FAILED] {context.Exception.Message}");
                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("CustomerOnly", policy => policy.RequireRole("customer"));
                options.AddPolicy("SellerOnly", policy => policy.RequireRole("seller"));
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
                options.AddPolicy("AnyAuthenticated", policy => policy.RequireAuthenticatedUser());
            });

            services.AddScoped<IDbConnection>(sp =>
                new NpgsqlConnection(configuration.GetConnectionString("DefaultConnection")));

            services.AddPersistenceServices(configuration);
            services.AddCommands();
            services.AddQueries();

            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblyContaining<Startup>();

            services.AddSwaggerGen(c =>
                {
                    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);

                    c.IncludeXmlComments(xmlPath);

                    c.SwaggerDoc("v1", new OpenApiInfo { Title = "OrderService", Version = "v1" });
                    c.AddSecurityDefinition(
                        "token",
                        new OpenApiSecurityScheme
                        {
                            Type = SecuritySchemeType.Http,
                            BearerFormat = "JWT",
                            Scheme = "Bearer",
                            In = ParameterLocation.Header,
                            Name = HeaderNames.Authorization
                        }
                    );

                    c.AddSecurityRequirement(
                        new OpenApiSecurityRequirement
                        {
                            {
                                new OpenApiSecurityScheme
                                {
                                    Reference = new OpenApiReference
                                    {
                                        Type = ReferenceType.SecurityScheme,
                                        Id = "token"
                                    },
                                },
                                Array.Empty<string>()
                            }
                        }
                    );
                }
            );

            services.AddKafkaIntegration(configuration);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
                runner.MigrateUp();
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Marketplace API v1"));
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            // Важно: порядок middleware имеет критическое значение
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}