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
            _logger.LogInformation("{@Request}", request);

            var response = await next();

            // Post Logic
            _logger.LogInformation("{@Request} ended with {response}.", request,  response);

            return response;
        }
    }
}

