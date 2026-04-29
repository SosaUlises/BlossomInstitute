using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Common.Features
{
    public class ResponseApiService
    {
        public static BaseResponseModel Response(int statusCode, object? Data = null, string? message = null)
        {
            bool success = false;

            if (statusCode >= 200 && statusCode < 300)
            {
                success = true;
            }

            var result = new BaseResponseModel()
            {
                Success = success,
                Data = Data,
                Message = message,
                StatusCode = statusCode
            };

            return result;
        }
    }
}
