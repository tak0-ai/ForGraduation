using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RuralTourism.Api.Migrations;
using RuralTourism.Api.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ????????? SignalR ???????
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

// ?????¡Â???
builder.Services.AddScoped<RuralTourism.Api.Services.IUserService, RuralTourism.Api.Services.UserService>();
builder.Services.AddSignalR();


// ???????? JSON ???§Ý?????
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

// ?????????????
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var sqlServerConn = builder.Configuration.GetConnectionString("SqlServer");
    // var sqliteConn = builder.Configuration.GetConnectionString("Sqlite");
    
    if (!string.IsNullOrWhiteSpace(sqlServerConn))
    {
        options.UseSqlServer(sqlServerConn);
    }
    // else if (!string.IsNullOrWhiteSpace(sqliteConn))
    // {
    //     options.UseSqlite(sqliteConn);
    // }
    else
    {
        throw new InvalidOperationException("No database connection string configured. Add 'ConnectionStrings:SqlServer' to appsettings.json.");
    }
});


builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<ITokenService, TokenService>();

// ???? JWT ???
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
        options.RequireHttpsMetadata = false; // ????????? false???????? true
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

// ??????????????
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

// ?§Þ?????????
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
                await context.Response.WriteAsJsonAsync(new { error = "????????????????????????????????????" });
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
    if (!db.Database.IsSqlite())
    {
        return;
    }

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
            // command.CommandText = "PRAGMA table_info('AppUsers');";
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
            // alter.CommandText = "ALTER TABLE AppUsers ADD COLUMN BannedUntil TEXT NULL;";
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
