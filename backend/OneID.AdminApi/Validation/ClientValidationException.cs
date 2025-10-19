using System.Collections.ObjectModel;

namespace OneID.AdminApi.Validation;

public sealed class ClientValidationException : Exception
{
    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Errors { get; }

    private ClientValidationException(
        string message,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> errors) : base(message)
    {
        Errors = errors;
    }

    public static ClientValidationException From(IDictionary<string, List<string>> errors)
    {
        var readonlyErrors = errors.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyCollection<string>)new ReadOnlyCollection<string>(kvp.Value));

        var message = string.Join("; ", readonlyErrors.Select(kvp => $"{kvp.Key}: {string.Join(',', kvp.Value)}"));

        return new ClientValidationException(message, new ReadOnlyDictionary<string, IReadOnlyCollection<string>>(readonlyErrors));
    }
}
