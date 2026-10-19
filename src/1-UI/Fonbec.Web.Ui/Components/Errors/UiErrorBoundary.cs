using System.Diagnostics;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;

namespace Fonbec.Web.Ui.Components.Errors;

public class UiErrorBoundary : ErrorBoundary
{
    private const string ReferenceKey = "Fonbec.ErrorReference";

    public UiErrorBoundary()
    {
        // A repeated failure must keep rendering the fallback. The base limit eventually rethrows
        // and tears down the circuit.
        MaximumErrorCount = int.MaxValue;
    }

    [Inject]
    public ILogger<UiErrorBoundary> Logger { get; set; } = null!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    public static string? ReadReference(Exception exception)
    {
        try
        {
            return exception.Data[ReferenceKey] as string;
        }
        catch (Exception)
        {
            return null;
        }
    }

    protected override Task OnErrorAsync(Exception exception)
    {
        var reference = Guid.NewGuid().ToString("N")[..8];
        try
        {
            exception.Data[ReferenceKey] = reference;
        }
        catch (Exception)
        {
            // Some exceptions reject changes to Data. The log still uses the local reference.
        }

        try
        {
            var (component, method) = FindUiFrame(exception);
            Logger.LogError(
                "Unhandled UI exception. ErrorReference: {ErrorReference} TraceId: {TraceId} Route: {Route} Component: {Component} Method: {Method} UserId: {UserId} ExceptionType: {ExceptionType} ExceptionMessage: {ExceptionMessage}",
                reference,
                CurrentTraceId(),
                SafeRoute(),
                component,
                method,
                SafeUserId(),
                exception.GetType().FullName ?? exception.GetType().Name,
                ExceptionLogSanitizer.RedactMessage(exception.Message));
        }
        catch (Exception)
        {
            // Logging must not take down the circuit.
        }

        return Task.CompletedTask;
    }

    private string SafeRoute()
    {
        try
        {
            return ExceptionLogSanitizer.StripQuery(NavigationManager.ToBaseRelativePath(NavigationManager.Uri));
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private string SafeUserId()
    {
        try
        {
            var authenticationState = AuthenticationStateProvider.GetAuthenticationStateAsync();
            if (!authenticationState.IsCompletedSuccessfully)
            {
                return string.Empty;
            }

            return authenticationState.Result.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private static string CurrentTraceId()
    {
        var traceId = Activity.Current?.TraceId ?? default;
        return traceId == default ? string.Empty : traceId.ToString();
    }

    private static (string Component, string Method) FindUiFrame(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            StackFrame[] frames;
            try
            {
                frames = new StackTrace(current, fNeedFileInfo: false).GetFrames() ?? [];
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var frame in frames)
            {
                var method = frame.GetMethod();
                if (method is null)
                {
                    continue;
                }

                var type = method.DeclaringType;
                var methodName = MethodName(method);
                while (type is not null && type.Name.Contains('<'))
                {
                    type = type.DeclaringType;
                }

                if (IsUiType(type))
                {
                    return (type!.Name, methodName);
                }
            }
        }

        return ("unknown", "unknown");
    }

    private static bool IsUiType(Type? type) =>
        type?.Namespace is not null
        && (type.Namespace == "Fonbec.Web.Ui"
            || type.Namespace.StartsWith("Fonbec.Web.Ui.", StringComparison.Ordinal));

    private static string MethodName(MethodBase method)
    {
        var declaringName = method.DeclaringType?.Name;
        if (declaringName is not null
            && declaringName.StartsWith('<')
            && !declaringName.StartsWith("<>", StringComparison.Ordinal))
        {
            var end = declaringName.IndexOf('>');
            if (end > 1)
            {
                return declaringName[1..end];
            }
        }

        return method.Name;
    }
}