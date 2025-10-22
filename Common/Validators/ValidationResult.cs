namespace MES.Common.Validators
{
    public sealed record ValidationResult
    {
        public List<string> Messages { get; } = [];
        public bool IsValid => Messages.Count == 0;
    }
}
