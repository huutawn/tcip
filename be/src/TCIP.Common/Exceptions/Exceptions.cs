namespace TCIP.Common.Exceptions;

public class BadRequestException(string message) : Exception(message);

public class ConflictException(string message) : Exception(message);

public class ForbiddenException(string message) : Exception(message);

public class NotFoundException(string message) : Exception(message);

public class PreconditionFailedException(string message) : Exception(message);

public class PreconditionRequiredException(string message) : Exception(message);

public class UnauthenticationException(string message) : Exception(message);

public class UnauthorizedException(string message) : Exception(message);
