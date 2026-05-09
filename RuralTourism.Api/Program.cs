using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RuralTourism.Api.Migrations;
using RuralTourism.Api.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 允许前端与 SignalR 跨域访问
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // SignalR needs this or specific origins when using AllowCredentials
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 注册应用服务
builder.Services.AddScoped<RuralTourism.Api.Services.IUserService, RuralTourism.Api.Services.UserService>();
builder.Services.AddSignalR();


// 控制器与 JSON 序列化设置
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

// 配置数据库连接
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var sqliteConn = builder.Configuration.GetConnectionString("Sqlite");
    

    if (!string.IsNullOrWhiteSpace(sqliteConn))
    {
        options.UseSqlite(sqliteConn);
    }
    else
    {
        // 没有配置连接串：抛出异常以便尽早发现
        throw new InvalidOperationException("No database connection string configured. Add 'ConnectionStrings:Sqlite' or 'ConnectionStrings:MySql' to appsettings.json.");
    }
});


builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<ITokenService, TokenService>();

// 配置 JWT 认证
var jwtKey = builder.Configuration["Jwt:Key"];
if (!string.IsNullOrWhiteSpace(jwtKey))
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // 本地开发设 false；生产设 true
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Issuer"]),
            ValidateAudience = !string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Audience"]),
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

    builder.Services.AddAuthorization();
}


var app = builder.Build();

// 启动时自动应用迁移
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    EnsureUserBanColumn(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseCors();

// 中间件与端点映射
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? context.User.FindFirst("sub")?.Value;
                     
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var db = context.RequestServices.GetRequiredService<ApplicationDbContext>();
            var bannedUntil = await db.AppUsers
                .Where(u => u.Id == userId)
                .Select(u => u.BannedUntil)
                .FirstOrDefaultAsync();

            if (bannedUntil.HasValue && bannedUntil.Value > DateTime.UtcNow)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.Headers.Append("X-Account-Banned", "true");
                await context.Response.WriteAsJsonAsync(new { error = "您的账户已被封禁，系统检测到异常登录并已终止会话。" });
                return; // Stop pipeline
            }
        }
    }
    await next();
});

app.MapControllers();
app.MapHub<RuralTourism.Api.Hubs.PostHub>("/hubs/post");
app.MapHub<RuralTourism.Api.Hubs.ChatHub>("/hubs/chat");

app.Run();

static void EnsureUserBanColumn(ApplicationDbContext db)
{
    var connection = db.Database.GetDbConnection();
    var shouldClose = connection.State != System.Data.ConnectionState.Open;

    try
    {
        if (shouldClose)
        {
            connection.Open();
        }

        var hasColumn = false;
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA table_info('AppUsers');";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var columnName = reader.GetString(1);
                if (string.Equals(columnName, "BannedUntil", StringComparison.OrdinalIgnoreCase))
                {
                    hasColumn = true;
                    break;
                }
            }
        }

        if (!hasColumn)
        {
            using var alter = connection.CreateCommand();
            alter.CommandText = "ALTER TABLE AppUsers ADD COLUMN BannedUntil TEXT NULL;";
            alter.ExecuteNonQuery();
        }
    }
    finally
    {
        if (shouldClose && connection.State == System.Data.ConnectionState.Open)
        {
            connection.Close();
        }
    }
}

