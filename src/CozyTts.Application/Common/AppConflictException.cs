namespace CozyTts.Application.Common;

public sealed class AppConflictException(string message) : Exception(message);
