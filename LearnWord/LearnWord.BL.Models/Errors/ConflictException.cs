namespace LearnWord.BL.Models.Errors;

public sealed class ConflictException : ApiException
{
    public ConflictException(string detail, string errorCode = "conflict")
        : base(409, "Conflict", errorCode, detail) { }
}
