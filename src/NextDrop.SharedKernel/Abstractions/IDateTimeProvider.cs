namespace NextDrop.SharedKernel.Abstractions;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
