using Microsoft.AspNetCore.Mvc.Filters;
using RuralTourism.Api.Entities;
using RuralTourism.Api.Migrations;
using System.Security.Claims;
using System.Text.Json;

namespace RuralTourism.Api.Filters
{
    public class AuditLogFilter : IAsyncActionFilter
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AuditLogFilter> _logger;

        public AuditLogFilter(ApplicationDbContext db, ILogger<AuditLogFilter> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await next();

            var httpContext = context.HttpContext;
            var request = httpContext.Request;
            var response = httpContext.Response;

            var userId = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                         ?? httpContext.User?.FindFirst("sub")?.Value 
                         ?? "Anonymous";

            var actionName = $"[{request.Method}] {request.Path}";

            var ipAddress = GetClientIpAddress(httpContext);

            var requestPayload = GetSanitizedRequestPayload(context);

            var operationLog = new OperationLog
            {
                UserId = userId,
                ActionName = actionName,
                IpAddress = ipAddress,
                RequestPayload = requestPayload,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                _db.OperationLogs.Add(operationLog);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save operation log for action: {ActionName}", actionName);
            }
        }

        private string GetClientIpAddress(HttpContext context)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private string? GetSanitizedRequestPayload(ActionExecutingContext context)
        {
            if (context.ActionDescriptor == null || context.HttpContext.Request.Method != "POST" && context.HttpContext.Request.Method != "PUT")
            {
                return null;
            }

            try
            {
                var payload = new Dictionary<string, object>();

                foreach (var arg in context.ActionArguments)
                {
                    var value = arg.Value;
                    if (value != null)
                    {
                        payload[arg.Key] = SanitizeObject(value);
                    }
                }

                if (payload.Count == 0)
                {
                    return null;
                }

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    MaxDepth = 3
                });

                if (json.Length > 2000)
                {
                    return json.Substring(0, 1997) + "...";
                }

                return json;
            }
            catch
            {
                return "[Payload serialization failed]";
            }
        }

        private object SanitizeObject(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            var type = obj.GetType();
            var properties = type.GetProperties();

            var result = new Dictionary<string, object?>();

            foreach (var prop in properties)
            {
                var propName = prop.Name.ToLower();
                var propValue = prop.GetValue(obj);

                if (propName.Contains("password") || propName.Contains("pwd") || propName.Contains("secret") || propName.Contains("token"))
                {
                    result[prop.Name] = "***REDACTED***";
                }
                else if (propValue != null && propValue.GetType().IsClass && propValue.GetType() != typeof(string))
                {
                    result[prop.Name] = "[Complex Object]";
                }
                else
                {
                    result[prop.Name] = propValue;
                }
            }

            return result;
        }
    }
}