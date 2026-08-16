using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DevStart.WebApi.Infrastructure
{
    /// <summary>
    /// Documents the X-Captcha-Token header on the endpoints that carry
    /// <see cref="RequiresCaptchaMetadata"/>. Swashbuckle cannot infer a header that no handler
    /// parameter binds to, so without this the requirement would be invisible in Swagger — the one
    /// real cost of transporting the token in a header instead of the request body.
    /// </summary>
    internal sealed class CaptchaHeaderOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            bool isProtected = context.ApiDescription.ActionDescriptor.EndpointMetadata
                .OfType<RequiresCaptchaMetadata>()
                .Any();

            if (!isProtected)
            {
                return;
            }

            operation.Parameters ??= [];
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = CaptchaEndpointFilter.HeaderName,
                In = ParameterLocation.Header,
                // Not marked required: enforcement is config-driven, and "required" would be a lie in
                // every environment that runs with Captcha:Enabled=false.
                Required = false,
                Description = "Yandex SmartCaptcha token. Required when Captcha:Enabled is true.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.String },
            });
        }
    }
}
