using FluentValidation;
using FluentValidation.AspNetCore;
using Mediatr.CQRS.Example.Behaviours;
using Mediatr.CQRS.Example.Database;
using Mediatr.CQRS.Example.Domain;
using Mediatr.CQRS.Example.Filters;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mediatr.CQRS.Example.Tests;

[SetUpFixture]
public class Testing
{
    private static IServiceScopeFactory? _scopeFactory;

    [OneTimeSetUp]
    public void RunBeforeAnyTests()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddSingleton<Repository>();

        builder.Services.AddControllers(options =>
            options.Filters.Add<ApiExceptionFilterAttribute>())
        .AddFluentValidation(x => x.AutomaticValidationEnabled = false);

        builder.Services.AddValidatorsFromAssembly(typeof(Mediatr.CQRS.Example.Commands.AddToDo).Assembly);

        builder.Services.AddMediatR(typeof(Mediatr.CQRS.Example.Commands.AddToDo).Assembly);
        builder.Services.AddMemoryCache();

        // Order of pipeline is important
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehaviour<,>));

        var app = builder.Build();

        app.MapControllers();

        _scopeFactory = builder.Services.BuildServiceProvider().GetService<IServiceScopeFactory>();

    }

    public static void ResetState()
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetService<Repository>();

        context?.ToDos.Clear();

    }

    public static async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        using var scope = _scopeFactory?.CreateScope();

        var mediator = scope?.ServiceProvider.GetService<IMediator>();

        return await mediator.Send(request);
    }

    public static ToDo? Find(int id)
    {
        using var scope = _scopeFactory?.CreateScope();

        var context = scope.ServiceProvider.GetService<Repository>();

        return context?.ToDos.Find(x => x.Id == id);
    }
}
