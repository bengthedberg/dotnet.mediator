# Mediator Pattern and CQRS

The Mediator pattern ensures that components are loosely coupled, such that they do not call each other explicitly, but instead do so through calls to a mediator.

## Problem

In traditional architectures, the same data model is used to query and update a database. That’s simple and works well for basic CRUD operations. In more complex applications, however, this approach can become unwieldy. For example, on the read side, the application may perform many different queries, returning data transfer objects (DTOs) with different shapes. Object mapping can become complicated. On the write side, the model may implement complex validation and business logic. As a result, you can end up with an overly complex model that does too much.

## Solution

CQRS ([Command Query Responsibility Segregation](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)) addresses separate reads and writes into separate models, using commands to update data, and queries to read data.
Commands should be task-based, rather than data-centric. (“Book hotel room,” not “set ReservationStatus to Reserved.”) Commands may be placed on a queue for asynchronous processing, rather than being processed synchronously.
Queries never modify the database. A query returns a DTO that does not encapsulate any domain knowledge.

## Implementation

[MediatR](https://github.com/jbogard/MediatR) is an open source implementation of the mediator pattern that doesn’t try to do too much and performs no magic. It allows you to compose messages, create and listen for events using synchronous or asynchronous patterns. It helps to reduce coupling and isolate the concerns of requesting the work to be done and creating the handler that dispatches the work.

### Example

#### CQRS 

All MediatR CQRS logic implements a Request, a RequestHandler and a Response. 

Install required nuget packages:

`Install-Package MediatR`  
`Install-Package MediatR.Extensions.Microsoft.DependencyInjection`


This example encapsulate the whole logic in a static class called `GetToDoById`. This will make it easier to understand what this query/cpommand does.

The request is basically either a Query or a Command. It must implement the `IRequest` interface with the required input data.  

```csharp
public static class GetToDoById
{
    // Query, all the data we need to execute the handler
    public record Query(int Id) : IRequest<Response>;

    // Handler, all the logic to execute, returns a response
    public class Handler : IRequestHandler<Query, Response>
    {
        private readonly Repository repository;

        public Handler(Repository repository)
        {
            this.repository = repository;
        }
        public async Task<Response?> Handle(Query request, CancellationToken cancellationToken)
        {
            var todo = repository.ToDos.FirstOrDefault(x => x.Id == request.Id);

            return todo is null ? null : new Response(todo.Id, todo.Name, todo.Completed);
        }
    }

    // Response, the data we want to return
    public record Response(int Id, String Name, bool Completed);
}
``` 

```csharp
public static class AddToDo
{
    public record Command(string Name) :IRequest<int>;

    public class Handler : IRequestHandler<Command, int>
    {
        private readonly Repository repository;

        public Handler(Repository repository)
        {
            this.repository = repository;
        }

        public async Task<int> Handle(Command request, CancellationToken cancellationToken)
        {
            var id = repository.NextId;
            repository.ToDos.Add(new ToDo { Id = id, Name = request.Name });
            return id;
        }
    }
}
```

The request and response are mutable so we use the new record type to enforce this.

The Handler implements MediatR `IRequestHandler` interface which takes a request (i.e. the query or command) and a response. 

The Response is the data we want to return to the caller. Again this is immutable so we use the `record` type.

Add dependecy injection in the program start up:

```csharp
builder.Services.AddMediatR(typeof(Program).Assembly);
```

Use the query from a controller:

```csharp
[Route("api/[controller]")]
[ApiController]
public class ToDoController : ControllerBase
{
    private readonly IMediator mediator;

    public ToDoController(IMediator mediator)
    {
        this.mediator = mediator;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetToDoById(int id)
    {
        var response = await mediator.Send(new GetToDoById.Query(id));
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("")]
    public async Task<IActionResult> AddToDo(AddToDo.Command command) 
        => Ok(await mediator.Send(command));    
}
```

#### Behaviour 

Let’s learn about MediatR Pipeline Behaviour in ASP.NET Core, the idea of Pipelines, How to intersect the pipeline and add various Services like Logging and Validations.

What are Pipelines? 

Requests/Responses travel back and forth through Pipelines in ASP.net core. When an Actor sends a request it passes the through a pipeline to the application, where an operation is performed using data contained in the request message. Once, the operation is completed a Response is sent back through the Pipeline containing the relevant data .

Pipelines are only aware of what the Request or Response are, and this is an important concept to understand when thinking about ASP.net Core Middleware.

Pipelines are also extremely handy when it comes to wanting implement common logic like Validation and Logging, primarily because we can write code that executes during the request enabling you to validate or perform logging duties etc.

**MediatR Pipeline Behaviour**  

MediatR Pipeline behaviours were introduced in Version 3, enabling you execute validation or logging logic before and after your Command or Query Handlers execute, resulting in your handlers only having to deal with Valid requests in your CQRS implementation, and you don’t have to clutter your Handler methods with repetitive logging or validation logic!

![MediatR Pipeline](.\images\MediatRPipeline.png)

Lets implement a behaviour for logging, create a new class called `LoggingBehaviour` that implements the `IPipelineBehavior` interface:

```csharp
using MediatR; 

namespace Mediatr.CQRS.Example.Behaviours;
public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{

    public async Task<TResponse> Handle(
        TRequest request, 
        CancellationToken cancellationToken, 
        RequestHandlerDelegate<TResponse> next)
    {
        // Pre Logic
        
        var response = await next();

        // Post Logic

        return response;
    }
}
``` 

This will now hook into the MediatR pipeline, so you can implement pre logic, execute the next handler (might be another pipeline or a command/request) then run some post logic.

Actual implementation of a logger pipeline:

```csharp
using MediatR;

namespace Mediatr.CQRS.Example.Behaviours;
public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ILogger<TRequest> _logger;

    public LoggingBehaviour(ILogger<TRequest> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request, 
        CancellationToken cancellationToken, 
        RequestHandlerDelegate<TResponse> next)
    {

        using (_logger.BeginScope(request))
        {
            // Pre Logic
            _logger.LogInformation("{Request}", request);

            var response = await next();

            // Post Logic
            _logger.LogInformation("{request} ended with {response}.", request,  response);

            return response;
        }
    }
}
```

Create constructor that takes the current logger implementation, 
Create a logging scope for the request and log the request and the response.

Add the pipeline to startup:

```csharp
// Order of pipeline is important
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
```


#### Caching

Using the pipeline behaviour we can also add a simple caching functionality:

Lets forst create an interface for cacheable query requests:

```csharp
namespace Mediatr.CQRS.Example.Caching;

public interface ICacheable
{
    string CacheKey { get; }            
}
``` 

Extend the GetToDoById query so it implements the cacheable interface

```csharp
...
 // Query, all the data we need to execute the handler
    public record Query(int Id) : IRequest<Response>, ICacheable
    {
        public string CacheKey => $"GetToDoById-{Id}";
    }
...
```

Note that we need to make the CacheKey unique across all queries so we add the query name as a prefix, then the actual request data.

Lets implement the cache behaviour:

```csharp
using Mediatr.CQRS.Example.Caching;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace Mediatr.CQRS.Example.Behaviours;

public class CachingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : ICacheable
{
    private readonly IMemoryCache cache;
    private readonly ILogger<CachingBehaviour<TRequest, TResponse>> logger;
    public CachingBehaviour(IMemoryCache cache, ILogger<CachingBehaviour<TRequest, TResponse>> logger)
    {
        this.cache = cache;
        this.logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        var requestName = request.GetType();
        logger.LogInformation("{Request} is configured for caching.", requestName);

        // Check to see if the item is inside the cache
        TResponse response;
        if (cache.TryGetValue(request.CacheKey, out response))
        {
            logger.LogInformation("Returning cached value for {Request}.", requestName);
            return response;
        }

        // Item is not in the cache, execute request and add to cache
        logger.LogInformation("{Request} Cache Key: {Key} is not inside the cache, executing request.", requestName, request.CacheKey);
        response = await next();
        cache.Set(request.CacheKey, response);
        return response;
    }
}
``` 

Add the behaviour in the startup:

```csharp
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehaviour<,>));
```

We also need to add an implementation for the IMemoryCache:

```csharp
builder.Services.AddMemoryCache();
```

#### Validation

Lets validate the request using [FluentValidation](https://docs.fluentvalidation.net/) library. Its good practice to 
validated them before the Handler processes them. 

In order to have this done we need to add some nuget packages:

- FluentValidation
- FluentValidation.AspNetCore
- FluentValidation.DependencyInjectionExtensions

Create a custom implementation of IPipelineBehavior:

```csharp
using FluentValidation;
using Mediatr.CQRS.Example.Exceptions;
using MediatR;

namespace Mediatr.CQRS.Example.Behaviours;
public class ValidationBehaviour<TRequest, TResponse> : 
    IPipelineBehavior<TRequest, TResponse>
     where TRequest : notnull
{
    private readonly ILogger<TRequest> _logger;

    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators, ILogger<TRequest> logger)
    {
        _logger = logger;
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, 
        CancellationToken cancellationToken, 
        RequestHandlerDelegate<TResponse> next)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(v =>
                    v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .Where(r => r.Errors.Any())
                .SelectMany(r => r.Errors)
                .ToList();

            if (failures.Any())
            {
                _logger.LogError($"{request} failed validation, {failures.Count} errors found.");
                foreach (var e in failures)
                    _logger.LogError("{property} : {error}", e.PropertyName, e.ErrorMessage);

                throw new RequestValidationException(failures);
            }
        }
        return await next();
    }
}
```

Add a validate class to the AddToDo command. This uses fluent validation.

```csharp
using FluentValidation;
using Mediatr.CQRS.Example.Database;
using Mediatr.CQRS.Example.Domain;
using MediatR;

namespace Mediatr.CQRS.Example.Commands;

public static class AddToDo
{
    public record Command(string Name) : IRequest<Response>;

    public class Validate : AbstractValidator<Command>
    {

        private readonly Repository _repository;

        public Validate(Repository repository)
        {
            _repository = repository;

            RuleFor(v => v.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(200).WithMessage("Name must not exceed 200 characters.")
                .Must(BeUniqueName).WithMessage("The specified name already exists.");
        }

        public bool BeUniqueName(string name)
        {
            return _repository.ToDos
                .All(x => x.Name != name);
        }
    }

    public class Handler : IRequestHandler<Command, Response>
    {
        private readonly Repository _repository;

        public Handler(Repository repository)
        {
            this._repository = repository;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            var id = _repository.NextId;
            _repository.ToDos.Add(new ToDo { Id = id, Name = request.Name });
            return new Response { Id = id };
        }
    }

    public record Response 
    {
        public int Id { get; set; }
    }
}
```

Configure the startup:

```csharp
...
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
...
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
...
```

Add a custom exception to hold failed validation results:

```csharp
using FluentValidation.Results;

namespace Mediatr.CQRS.Example.Exceptions;

public class RequestValidationException : Exception
{
    public RequestValidationException()
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public RequestValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }

    public IDictionary<string, string[]> Errors { get; }
}
```

Add a ASP Pipeline filter to handle exceptions, etc:

```csharp
using Mediatr.CQRS.Example.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mediatr.CQRS.Example.Filters;

public class ApiExceptionFilterAttribute : ExceptionFilterAttribute
{

    private readonly IDictionary<Type, Action<ExceptionContext>> _exceptionHandlers;

    public ApiExceptionFilterAttribute()
    {
        // Register known exception types and handlers.
        _exceptionHandlers = new Dictionary<Type, Action<ExceptionContext>>
            {
                { typeof(RequestValidationException), HandleValidationException },
                { typeof(NotFoundException), HandleNotFoundException },
                { typeof(UnauthorizedAccessException), HandleUnauthorizedAccessException },
//                { typeof(ForbiddenAccessException), HandleForbiddenAccessException },
            };
    }

    public override void OnException(ExceptionContext context)
    {
        HandleException(context);

        base.OnException(context);
    }

    private void HandleException(ExceptionContext context)
    {
        Type type = context.Exception.GetType();
        if (_exceptionHandlers.ContainsKey(type))
        {
            _exceptionHandlers[type].Invoke(context);
            return;
        }

        if (!context.ModelState.IsValid)
        {
            HandleInvalidModelStateException(context);
            return;
        }

        HandleUnknownException(context);
    }

    private void HandleValidationException(ExceptionContext context)
    {
        var exception = (RequestValidationException)context.Exception;

        var details = new ValidationProblemDetails(exception.Errors)
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        };

        context.Result = new BadRequestObjectResult(details);

        context.ExceptionHandled = true;
    }

    private void HandleInvalidModelStateException(ExceptionContext context)
    {
        var details = new ValidationProblemDetails(context.ModelState)
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        };

        context.Result = new BadRequestObjectResult(details);

        context.ExceptionHandled = true;
    }

    private void HandleNotFoundException(ExceptionContext context)
    {
        var exception = (NotFoundException)context.Exception;

        var details = new ProblemDetails()
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "The specified resource was not found.",
            Detail = exception.Message
        };

        context.Result = new NotFoundObjectResult(details);

        context.ExceptionHandled = true;
    }

    private void HandleUnauthorizedAccessException(ExceptionContext context)
    {
        var details = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
        };

        context.Result = new ObjectResult(details)
        {
            StatusCode = StatusCodes.Status401Unauthorized
        };

        context.ExceptionHandled = true;
    }

    private void HandleForbiddenAccessException(ExceptionContext context)
    {
        var details = new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3"
        };

        context.Result = new ObjectResult(details)
        {
            StatusCode = StatusCodes.Status403Forbidden
        };

        context.ExceptionHandled = true;
    }

    private void HandleUnknownException(ExceptionContext context)
    {
        var details = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An error occurred while processing your request.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };

        context.Result = new ObjectResult(details)
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };

        context.ExceptionHandled = true;
    }
}
```

Hook it all up in the startup:

```csharp
builder.Services.AddControllers(options =>
            options.Filters.Add<ApiExceptionFilterAttribute>())
    .AddFluentValidation(x => x.AutomaticValidationEnabled = false);
```


