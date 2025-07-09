namespace SampleProject.Core.Models.Entities;

public class EntityBase<TId>
{
	protected EntityBase(TId id)
	{
		Id = id;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = null;
    }

	protected EntityBase()
	{
	}

	public TId Id { get; }
	public DateTimeOffset CreatedAtUtc { get; set; }
	public DateTimeOffset? UpdatedAtUtc { get; set; }
}
