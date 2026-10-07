using Carroza.Api.Contracts.DTOs;
using Carroza.Api.Domain.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Carroza.Api.Infrastructure.Http;

// Reads and global stop can now fail at an external boundary as well as commands.
public sealed class ComponentExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ComponentOperationException error) return;
        var status = error.Code switch
        {
            "DEVICE_OFFLINE" => 503, "TIMEOUT" => 504,
            "INVALID_COMMAND" or "INVALID_PARAMETER" => 400, "OPERATION_CONFLICT" => 409, _ => 500
        };
        context.Result = new ObjectResult(new CommandErrorDto(status, error.Code, error.Message)) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}
