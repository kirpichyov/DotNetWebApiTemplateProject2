namespace SampleProject.Core.Contracts;

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAtUtc { get; set; }
    string DeletedBy { get; set; }
    
    void MarkAsDeleted(string deletedBy, DateTimeOffset deletedAtUtc)
    {
        IsDeleted = true;
        DeletedAtUtc = deletedAtUtc;
        DeletedBy = deletedBy;
    }
}