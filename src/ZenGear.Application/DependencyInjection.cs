using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ZenGear.Application.Common.Behaviours;

namespace ZenGear.Application;

/// <summary>
/// Dependency injection configuration for Application layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add Application layer services to DI container.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR - CQRS pattern
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Pipeline behaviors (order matters!)
            // 1. Exception handling (outermost)
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
            
            // 2. Validation
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
            
            // 3. Performance monitoring
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));
            
            // 4. Logging
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
        });

        // FluentValidation - automatic validator discovery
        services.AddValidatorsFromAssembly(assembly);

        // AutoMapper - DTO mapping (discovers all Profile classes in assembly)
        services.AddAutoMapper(cfg =>
        {
            cfg.AddMaps(assembly);
        });

        return services;
    }
}
