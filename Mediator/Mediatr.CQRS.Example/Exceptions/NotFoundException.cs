﻿namespace Mediatr.CQRS.Example.Exceptions;
public class NotFoundException : Exception
{
    public NotFoundException()
        : base()
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public NotFoundException(string request, string name, object key)
        : base($"{request} Entity \"{name}\" ({key}) was not found.")
    {
    }
}
