namespace xiaomistep.Models
{
    public class LogModel
    {
        public string? Message { get; set; }
        public DateTime? Time { get; set; }
        public Level? Level { get; set; }
    }
    public enum Level
    {
        None = 0,
        Error=1,
        Warning=2,
        Info=3,
        Success=4,
    }
}
