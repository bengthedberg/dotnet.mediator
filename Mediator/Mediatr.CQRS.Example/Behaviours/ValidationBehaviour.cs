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
                _logger.LogError("{@Request} failed validation, {failures} errors found.", request, failures.Count);
                foreach (var e in failures)
                    _logger.LogError("{property} : {error}", e.PropertyName, e.ErrorMessage);

                throw new RequestValidationException(failures);
            }
        }
        return await next();
    }
}
