namespace MES.Common.Validators
{
    public abstract class OptionsValidator
    {
        protected static bool TryValidateProperty(string name, string value, out string validationMessage)
        {
            validationMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(value))
            {
                validationMessage = $"{name} cannot be empty or whitespace.";

                return false;
            }

            if (!char.IsLetter(value[0]))
            {
                validationMessage = $"{name} must start with a letter.";

                return false;
            }

            return true;
        }

        protected static string FormatMessage(string description, params IEnumerable<string> values)
            => $"{description}: {string.Join(", ", values)}";
    }
}
