namespace ScurryDashboard.Models
{
    public class ApiResult<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public T? Data { get; set; }

        public static ApiResult<T> Ok(T data, string msg = "Success")
            => new() { Success = true, Message = msg, Data = data };
        public static ApiResult<T> Fail(string msg)
            => new() { Success = false, Message = msg, Data = default };
    }
}
