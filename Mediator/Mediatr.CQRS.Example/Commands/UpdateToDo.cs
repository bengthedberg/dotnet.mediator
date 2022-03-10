using FluentValidation;
using Mediatr.CQRS.Example.Database;
using Mediatr.CQRS.Example.Domain;
using Mediatr.CQRS.Example.Exceptions;
using MediatR;

namespace Mediatr.CQRS.Example.Commands;

public static class UpdateToDo
{
    public record Command(int Id, string Name, bool Completed) : IRequest;

    public class Validate : AbstractValidator<Command>
    {

        private readonly Repository _repository;

        public Validate(Repository repository)
        {
            _repository = repository;

            RuleFor(v => v.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");
            RuleFor(v => v.Id)
                .NotNull().WithMessage("Id is required");
        }
    }

    public class Handler : IRequestHandler<Command>
    {
        private readonly Repository _repository;

        public Handler(Repository repository)
        {
            _repository = repository;
        }

        public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
        {
            var todoItem = _repository.ToDos.FirstOrDefault(x => x.Id == request.Id);

            if (todoItem is null)
            {
                throw new NotFoundException("UpdateToDo", nameof(ToDo), request.Id);
            }

            todoItem.Name = request.Name;
            todoItem.Completed = request.Completed;

            return Unit.Value;
        }
    }
}
