using Mediatr.CQRS.Example.Caching;
using Mediatr.CQRS.Example.Database;
using MediatR;

namespace Mediatr.CQRS.Example.Queries;

public static class GetToDoById
{
    // Query, all the data we need to execute the handler
    public record Query(int Id) : IRequest<Response>, ICacheable
    {
        public string CacheKey => $"GetToDoById-{Id}";
    }

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

            return todo is null ? null : new Response { Id = todo.Id, Name = todo.Name, Completed = todo.Completed };
        }
    }

    // Response, the data we want to return
    public record Response  
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public bool Completed { get; set; }
    }
}

