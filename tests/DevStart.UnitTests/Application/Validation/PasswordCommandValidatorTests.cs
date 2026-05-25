using DevStart.Application;
using DevStart.Application.Users.ChangePassword;
using DevStart.Application.Users.ResetPassword;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DevStart.UnitTests.Application.Validation
{
    public sealed class PasswordCommandValidatorTests
    {
        private readonly ServiceProvider _serviceProvider = new ServiceCollection()
            .AddApplication()
            .BuildServiceProvider();

        [Fact]
        public void ResetPasswordValidator_RequiresNewPasswordMinLength8()
        {
            IValidator<ResetPasswordCommand> validator =
                _serviceProvider.GetRequiredService<IValidator<ResetPasswordCommand>>();

            validator.Validate(new ResetPasswordCommand(Guid.NewGuid(), "longenough8")).IsValid.ShouldBeTrue();
            validator.Validate(new ResetPasswordCommand(Guid.NewGuid(), "short")).IsValid.ShouldBeFalse();
            validator.Validate(new ResetPasswordCommand(Guid.NewGuid(), "")).IsValid.ShouldBeFalse();
        }

        [Fact]
        public void ChangePasswordValidator_RequiresCurrentAndNewPasswordMinLength8()
        {
            IValidator<ChangePasswordCommand> validator =
                _serviceProvider.GetRequiredService<IValidator<ChangePasswordCommand>>();

            validator.Validate(new ChangePasswordCommand("current", "longenough8")).IsValid.ShouldBeTrue();
            validator.Validate(new ChangePasswordCommand("", "longenough8")).IsValid.ShouldBeFalse();
            validator.Validate(new ChangePasswordCommand("current", "short")).IsValid.ShouldBeFalse();
        }
    }
}
