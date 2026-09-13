using GenericRepository.Utilities;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace GenericRepository.ResualtFormat
{
    public class ResualValueFormat
    {
        public bool IsSuccess { get; set; }
        public ApiResultStatusCode StatusCode { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Message { get; set; }

        public ResualValueFormat(bool isSuccess, ApiResultStatusCode statusCode, string message = null)
        {
            IsSuccess = isSuccess;
            StatusCode = statusCode;
            Message = message ?? statusCode.ToDisplay();
        }

        #region Implicit Operators
        public static implicit operator ResualValueFormat(OkResult result)
        {
            return new ResualValueFormat(true, ApiResultStatusCode.Success);
        }

        public static implicit operator ResualValueFormat(BadRequestResult result)
        {
            return new ResualValueFormat(false, ApiResultStatusCode.BadRequest);
        }

        public static implicit operator ResualValueFormat(BadRequestObjectResult result)
        {
            var message = result.Value.ToString();
            if (result.Value is SerializableError errors)
            {
                var errorMessages = errors.SelectMany(p => (string[])p.Value).Distinct();
                message = string.Join(" | ", errorMessages);
            }
            return new ResualValueFormat(false, ApiResultStatusCode.BadRequest, message);
        }

        public static implicit operator ResualValueFormat(ContentResult result)
        {
            return new ResualValueFormat(true, ApiResultStatusCode.Success, result.Content);
        }

        public static implicit operator ResualValueFormat(NotFoundResult result)
        {
            return new ResualValueFormat(false, ApiResultStatusCode.NotFound);
        }
        #endregion
    }

    public class ResualValueFormat<TData> : ResualValueFormat
        where TData : class
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TData Data { get; set; }

        public ResualValueFormat(bool isSuccess, ApiResultStatusCode statusCode, TData data, string message = null)
            : base(isSuccess, statusCode, message)
        {
            Data = data;
        }

        #region Implicit Operators
        public static implicit operator ResualValueFormat<TData>(TData data)
        {
            if(data == null)
                new ResualValueFormat(false, ApiResultStatusCode.ListEmpty, ApiResultStatusCode.ListEmpty.ToDisplay());
            return new ResualValueFormat<TData>(true, ApiResultStatusCode.Success, data);
        }
     
        #endregion
    }
}
