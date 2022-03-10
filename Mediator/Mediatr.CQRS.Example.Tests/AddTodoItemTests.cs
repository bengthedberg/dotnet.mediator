using FluentAssertions;
using Mediatr.CQRS.Example.Commands;
using Mediatr.CQRS.Example.Exceptions;
using NUnit.Framework;

namespace Mediatr.CQRS.Example.Tests;

using static Testing;
public class AddTodoItemTests : TestBase
{
    [Test]
    public void ShouldCreateTodoItem()
    {

        var response = SendAsync(new AddToDo.Command
        {
            Name = "New Task"
        });

        var item = Find(response.Result.Id);

        item.Should().NotBeNull();
        item.Id.Should().Be(response.Result.Id);
        item.Name.Should().Be("New Task");
        item.Completed.Should().Be(false);
    }
    [Test]
    public void  ShouldRequireMinimumFields()
    {
        var command = new AddToDo.Command
        {
            Name = ""
        };
        
        var response = SendAsync(command);

        response.Should().NotBeNull();
        response.Exception.Should().NotBeNull();
        response.Exception.InnerException.Should().BeOfType<RequestValidationException>();
        response.Exception.InnerException.As<RequestValidationException>().Errors.Should().ContainKey("Name").WhoseValue.Should().BeEquivalentTo(new string[] { "Name is required." });
    }

    [Test]
    public void ShouldNotCreateDuplicateTodoItem()
    {

        var command = new AddToDo.Command
        {
            Name = "Duplicate Task"
        };

        var response = SendAsync(command);
        response = SendAsync(command);

        response.Should().NotBeNull();
        response.Exception.Should().NotBeNull();
        response.Exception.InnerException.Should().BeOfType<RequestValidationException>();
        response.Exception.InnerException.As<RequestValidationException>().Errors.Should().ContainKey("Name").WhoseValue.Should().BeEquivalentTo(new string[] { "The specified name already exists." });

    }
}