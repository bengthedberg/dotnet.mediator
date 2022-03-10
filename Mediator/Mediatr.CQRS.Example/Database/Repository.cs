using Mediatr.CQRS.Example.Domain;

namespace Mediatr.CQRS.Example.Database;

public class Repository
{
    public int NextId => ToDos.Count() > 0 ? ToDos.Max(x => x.Id) + 1 : 1;
    public List<ToDo> ToDos { get; } = new List<ToDo>
    {
        new ToDo{Id = 1, Name = "Cook dinner", Completed = false },
        new ToDo{Id = 2, Name = "Make Youtube video", Completed = true },
        new ToDo{Id = 3, Name = "Wash car", Completed = false },
        new ToDo{Id = 4, Name = "Practice programming", Completed = true },
        new ToDo{Id = 5, Name = "Take out garbage", Completed = false },
    };
}
