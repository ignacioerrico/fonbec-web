using Fonbec.Web.DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using MlkPwgen;

namespace Fonbec.Web.Logic.Util;

public interface IPasswordGeneratorWrapper
{
    Task<string> GeneratePassword();
}

public class PasswordGeneratorWrapper(IConfiguration configuration, IUserRepository userRepository) : IPasswordGeneratorWrapper
{
    public async Task<string> GeneratePassword()
    {
        var length = configuration.GetValue<int>("Identity:Password:RequiredLength");
        var requireUppercase = configuration.GetValue<bool>("Identity:Password:RequireUppercase");
        var requireLowercase = configuration.GetValue<bool>("Identity:Password:RequireLowercase");
        var requireDigit = configuration.GetValue<bool>("Identity:Password:RequireDigit");
        var requireNonAlphanumeric = configuration.GetValue<bool>("Identity:Password:RequireNonAlphanumeric");

        var requiredSets = new List<string>();

        if (requireUppercase)
        {
            requiredSets.Add(Sets.Upper);
        }

        if (requireLowercase)
        {
            requiredSets.Add(Sets.Lower);
        }

        if (requireDigit)
        {
            requiredSets.Add(Sets.Digits);
        }

        if (requireNonAlphanumeric)
        {
            requiredSets.Add(Sets.Symbols);
        }

        string pronounceablePassword;
        bool isPasswordValid;
        do
        {
            pronounceablePassword = PronounceableGenerator.Generate(length, requiredSets);
            (isPasswordValid, _) = await userRepository.ValidatePasswordAsync(pronounceablePassword);
        } while (!isPasswordValid);

        return pronounceablePassword;
    }
}