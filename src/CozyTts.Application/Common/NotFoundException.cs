namespace CozyTts.Application.Common;

public sealed class NotFoundException(string message) : Exception(message);
