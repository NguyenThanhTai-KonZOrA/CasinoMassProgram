namespace CasinoMassProgram.Middleware
{
    public class BaseApiResponse<T>
    {
        public int status { get; set; }
        public T data { get; set; }
        public bool success { get; set; }
    }
}
