namespace Mediatr.CQRS.Example.Domain;

public class ToDo
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool Completed { get; set; }
}


