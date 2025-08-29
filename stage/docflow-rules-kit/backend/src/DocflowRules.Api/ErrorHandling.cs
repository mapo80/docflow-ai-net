using Microsoft.AspNetCore.Mvc;

namespace DocflowRules.Api;

public static class ErrorHandling
{
    public static void UseProblemDetails(this IApplicationBuilder app)
    {
        app.Use(async (ctx, next) =>
        {
            try { await next(); }
            catch (FluentValidation.ValidationException ex)
            {
                var pd = new ProblemDetails { Title = "Validation error", Status = StatusCodes.Status400BadRequest, Detail = string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)) };
                ctx.Response.StatusCode = pd.Status ?? 400;
                await ctx.Response.WriteAsJsonAsync(pd);
            }
            catch (Exception ex)
            {
                var pd = new ProblemDetails { Title = "Server error", Status = StatusCodes.Status500InternalServerError, Detail = ex.Message };
                ctx.Response.StatusCode = pd.Status ?? 500;
                await ctx.Response.WriteAsJsonAsync(pd);
            }
        });
    }
}
