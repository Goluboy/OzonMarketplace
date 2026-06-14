using System.Security.Claims;
using System.Text.Json;
using Core.Minio;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ProductService.Application;
using ProductService.Infrastructure;
using ProductService.Infrastructure.Persistence;
using Prometheus;
using Redis;
using Serilog;

namespace ProductService.Presentation;

public static class Program
{
    private const string ReactCorsPolicy = "ReactAppPolicy";
    
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Host.UseSerilog((context, configuration) => 
            configuration.ReadFrom.Configuration(context.Configuration));
            
        var services = builder.Services;
        
        services.AddControllers();

        ConfigureServices(services, builder.Configuration);
        
        var app = builder.Build();
        
        app.Configuration.RunMigrations();
            
        app.ConfigureMiddleware();
            
        app.MapControllers();
        
        app.MapMetrics(); 
        
        app.Run();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor(); 
        
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

        services.AddCors(options =>
        {
            options.AddPolicy(ReactCorsPolicy, policy =>
            {
                if (allowedOrigins != null && allowedOrigins.Length != 0)
                {
                    policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
            });
        });

        
        services.AddInfrastructure(configuration)
            .AddApplication()
            .AddPresentation()
            .AddKeycloakAuthentication(configuration) 
            .AddMinioStorage(options =>
            {
                configuration.GetSection("Minio").Bind(options);
            })
            .AddRedisCache(options =>
            {
                configuration.GetSection("Redis").Bind(options);
            })
            .AddObservability(configuration);
    }
    
    private static void ConfigureMiddleware(this IApplicationBuilder builder)
    {
        builder.UseExceptionHandler();
        
        builder.UseHttpMetrics();
        
        builder.UseCors(ReactCorsPolicy);
        
        builder.UseAuthentication();
        builder.UseAuthorization();
        
        builder.UseSwagger();
        builder.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Marketplace API v1");
            options.RoutePrefix = "swagger";
        });
    }

    private static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"] 
                           ?? throw new NullReferenceException("OpenTelemetry Otlp Endpoint not found.");

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("ProductService"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.Filter = httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/swagger") &&
                        !httpContext.Request.Path.StartsWithSegments("/metrics");
                })
                .AddHttpClientInstrumentation()
                .AddNpgsql()
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                }));
        
        return services;
    }
    
    private static IServiceCollection AddKeycloakAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var keycloakAuthority = configuration["Keycloak:Authority"];
        var audience = configuration["Keycloak:Audience"];
        var validIssuer = configuration["Keycloak:ValidIssuer"] ?? "http://localhost:8080/realms/marketplace";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = keycloakAuthority;
                options.Audience = audience;
                options.RequireHttpsMetadata = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = validIssuer,

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
                        if (identity == null)
                        {
                            return Task.CompletedTask;
                        }

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

        services.AddAuthorizationBuilder()
            .AddPolicy("CustomerOnly", policy => policy.RequireRole("customer"))
            .AddPolicy("SellerOnly", policy => policy.RequireRole("seller"))
            .AddPolicy("AdminOnly", policy => policy.RequireRole("admin"))
            .AddPolicy("SellerOrAdmin", policy => policy.RequireRole("seller", "admin"))
            .AddPolicy("AnyAuthenticated", policy => policy.RequireAuthenticatedUser());

        return services;
    }
}