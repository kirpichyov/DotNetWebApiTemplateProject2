using SampleProject.Core.Contracts;

namespace SampleProject.Core.Models.Entities;

public abstract class AuditEntity<T> : EntityBase<T>, IAuditEntity
{
    protected AuditEntity(T id)
        : base(id)
    {
    }

    protected AuditEntity()
    {
    }
    
    public string CreatedBy { get; set; }
    public string UpdatedBy { get; set; }
}