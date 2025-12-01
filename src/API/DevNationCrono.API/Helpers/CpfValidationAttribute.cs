using System.ComponentModel.DataAnnotations;

namespace DevNationCrono.API.Helpers;

public class CpfValidationAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value == null)
            return new ValidationResult("CPF é obrigatório");

        string cpf = value.ToString();

        if (string.IsNullOrWhiteSpace(cpf))
            return new ValidationResult("CPF é obrigatório");

        if (!CpfValidator.Validar(cpf))
            return new ValidationResult("CPF inválido");

        return ValidationResult.Success;
    }
}
