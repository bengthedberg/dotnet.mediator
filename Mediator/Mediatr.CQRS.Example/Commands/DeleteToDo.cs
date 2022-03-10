using Mediatr.CQRS.Example.Database;
using Mediatr.CQRS.Example.Domain;
using Mediatr.CQRS.Example.Exceptions;
using MediatR;

namespace Mediatr.CQRS.Example.Commands;

public static class DeleteToDo
{
    public record Command : IRequest
    {
        public int Id { get; set; } 
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
                throw new NotFoundException("DeleteToDo", nameof(ToDo), request.Id);
            }

            _repository.ToDos.Remove(todoItem);

            return Unit.Value;
        }
    }
}
