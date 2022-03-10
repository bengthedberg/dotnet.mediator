using FluentValidation;
using Mediatr.CQRS.Example.Database;
using Mediatr.CQRS.Example.Domain;
using MediatR;

namespace Mediatr.CQRS.Example.Commands;

public static class AddToDo
{
    public record Command() : IRequest<Response>
    {
        public string Name { get; set; } = "";
    }

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

