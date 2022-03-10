using Mediatr.CQRS.Example.Commands;
using Mediatr.CQRS.Example.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Mediatr.CQRS.Example.Controllers;

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

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, UpdateToDo.Command command)
    {
        if (id != command.Id)
        {
            return BadRequest();
        }

        await mediator.Send(command);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        await mediator.Send(new DeleteToDo.Command { Id = id });

        return NoContent();
    }
}

