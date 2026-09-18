using System.ComponentModel.DataAnnotations;

namespace GenericRepository.ResualFormat
{
    public enum ApiResultStatusCode
    {
        [Display(Name = "Operation is success")]
        Success = 0,

        [Display(Name = "Operation is faild from server")]
        ServerError = 1,

        [Display(Name = "Operatio is faild, Invalid input parameters")]
        BadRequest = 2,

        [Display(Name = "Not found Data")]
        NotFound = 3,

        [Display(Name = "No Data, you have no data")]
        ListEmpty = 4,

        [Display(Name = "خطایی در پردازش رخ داد")]
        LogicError = 5,

        [Display(Name = "خطای احراز هویت")]
        UnAuthorized = 6
    }
}
