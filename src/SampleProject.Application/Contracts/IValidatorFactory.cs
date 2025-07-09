using FluentValidation;

namespace SampleProject.Application.Contracts;

public interface IValidatorFactory
{
    IValidator<TModel> GetFor<TModel>()
        where TModel : class;
    
    void ValidateAndThrow<TModel>(TModel model)
        where TModel : class;
}