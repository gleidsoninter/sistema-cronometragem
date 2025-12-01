using DevNationCrono.API.Exceptions;
using System.Net;
using System.Text.Json;

namespace DevNationCrono.API.Middlewares;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ErrorResponse();

        switch (exception)
        {
            case ValidationException validationEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.StatusCode = 400;
                errorResponse.Message = validationEx.Message;
                errorResponse.ErrorType = "ValidationError";
                _logger.LogWarning(validationEx, "Erro de validação: {Message}", validationEx.Message);
                break;

            case NotFoundException notFoundEx:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.StatusCode = 404;
                errorResponse.Message = notFoundEx.Message;
                errorResponse.ErrorType = "NotFound";
                _logger.LogWarning(notFoundEx, "Recurso não encontrado: {Message}", notFoundEx.Message);
                break;

            case UnauthorizedAccessException unauthorizedEx:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.StatusCode = 401;
                errorResponse.Message = "Não autorizado";
                errorResponse.ErrorType = "Unauthorized";
                _logger.LogWarning(unauthorizedEx, "Acesso não autorizado");
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.StatusCode = 500;
                errorResponse.Message = "Erro interno do servidor";
                errorResponse.ErrorType = "InternalError";
                _logger.LogError(exception, "Erro não tratado: {Message}", exception.Message);
                break;
        }

        errorResponse.Timestamp = DateTime.UtcNow;
        errorResponse.Path = context.Request.Path;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var result = JsonSerializer.Serialize(errorResponse, jsonOptions);
        await response.WriteAsync(result);
    }
}

public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; }
    public string ErrorType { get; set; }
    public DateTime Timestamp { get; set; }
    public string Path { get; set; }
}
