using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Server.Envir;
using Server.WebApi.Auth;
using Server.WebApi.Services;
using Server.WebApi.Endpoints;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Server.WebApi
{
    /// <summary>
    /// WebAPI startup configuration
    /// </summary>
    public static class WebApiStartup
    {
        private static WebApplication? _app;
        private static Task? _runTask;

        /// <summary>
        /// Start the WebAPI server
        /// </summary>
        public static void Start()
        {
            if (!Config.WebApiEnabled)
            {
                SEnvir.Log("WebAPI is disabled in configuration.");
                return;
            }

            try
            {
                // Determine wwwroot path - check multiple locations
                string wwwrootPath = FindWwwrootPath();

                var builder = WebApplication.CreateBuilder(new WebApplicationOptions
                {
                    ContentRootPath = Path.GetDirectoryName(wwwrootPath) ?? Environment.CurrentDirectory,
                    WebRootPath = wwwrootPath
                });

                // Configure Kestrel to listen on specified port
                builder.WebHost.ConfigureKestrel(options =>
                {
                    options.ListenAnyIP(Config.WebApiPort);
                });

                // Add services
                ConfigureServices(builder.Services);

                _app = builder.Build();

                // Configure middleware
                ConfigureMiddleware(_app);

                // Map endpoints
                MapEndpoints(_app);

                // Run in background
                _runTask = Task.Run(async () =>
                {
                    try
                    {
                        SEnvir.Log($"WebAPI server starting on port {Config.WebApiPort}...");
                        await _app.RunAsync();
                    }
                    catch (Exception ex)
                    {
                        SEnvir.Log($"WebAPI server error: {ex.Message}");
                    }
                });

                SEnvir.Log($"WebAPI server started on http://0.0.0.0:{Config.WebApiPort}");
            }
            catch (Exception ex)
            {
                SEnvir.Log($"Failed to start WebAPI server: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop the WebAPI server
        /// </summary>
        public static async Task StopAsync()
        {
            if (_app != null)
            {
                await _app.StopAsync();
                _app = null;
                SEnvir.Log("WebAPI server stopped.");
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Add CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // Add JWT Authentication
            var key = Encoding.UTF8.GetBytes(Config.WebApiJwtSecret);
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = "ZirconLegendServer",
                        ValidAudience = "ZirconWebUI",
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ClockSkew = TimeSpan.Zero
                    };
                });

            services.AddAuthorization();

            // Configure JSON serialization with camelCase naming
            services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
            {
                options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.SerializerOptions.WriteIndented = true;
            });

            // Add singleton services
            services.AddSingleton<JwtHelper>();
            services.AddSingleton<ServerDataService>();
            services.AddSingleton<ConfigService>();
        }

        private static void ConfigureMiddleware(WebApplication app)
        {
            app.UseCors("AllowAll");

            // Serve static files from wwwroot
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseAuthorization();
        }

        private static void MapEndpoints(WebApplication app)
        {
            // Handle favicon request to avoid 404
            app.MapGet("/favicon.ico", () => Results.NoContent());

            // Map all API endpoints
            AuthEndpoints.Map(app);
            DashboardEndpoints.Map(app);
            PlayerEndpoints.Map(app);
            AccountEndpoints.Map(app);
            CharacterEndpoints.Map(app);
            GameDataEndpoints.Map(app);
            LogEndpoints.Map(app);
            ConfigEndpoints.Map(app);

            // Fallback to index.html for SPA routing
            app.MapFallbackToFile("index.html");
        }

        /// <summary>
        /// Find wwwroot path from multiple possible locations
        /// </summary>
        private static string FindWwwrootPath()
        {
            var possiblePaths = new[]
            {
                // 1. Same directory as executable
                Path.Combine(AppContext.BaseDirectory, "wwwroot"),
                // 2. Server/wwwroot relative to current directory
                Path.Combine(Environment.CurrentDirectory, "Server", "wwwroot"),
                // 3. wwwroot relative to current directory
                Path.Combine(Environment.CurrentDirectory, "wwwroot"),
                // 4. Relative to executable location
                Path.Combine(Path.GetDirectoryName(typeof(WebApiStartup).Assembly.Location) ?? "", "wwwroot")
            };

            foreach (var path in possiblePaths)
            {
                if (Directory.Exists(path))
                {
                    SEnvir.Log($"Found wwwroot at: {path}");
                    return path;
                }
            }

            // Default fallback
            string defaultPath = Path.Combine(Environment.CurrentDirectory, "wwwroot");
            SEnvir.Log($"wwwroot not found, using default: {defaultPath}");
            return defaultPath;
        }
    }
}
