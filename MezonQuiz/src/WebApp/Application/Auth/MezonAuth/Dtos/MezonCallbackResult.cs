namespace WebApp.Application.Auth.MezonAuth.Dtos
{
    public class MezonCallbackResult
    {
        public int StatusCode { get; init; }
        public object Payload { get; init; } = default!;

        public static MezonCallbackResult Success(object payload)
        {
            return new MezonCallbackResult
            {
                StatusCode = StatusCodes.Status200OK,
                Payload = payload
            };
        }

        public static MezonCallbackResult Failure(int statusCode, object payload)
        {
            return new MezonCallbackResult
            {
                StatusCode = statusCode,
                Payload = payload
            };
        }
    }
}