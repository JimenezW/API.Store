namespace API.Store.API.Configurations
{
    public class Security_Scret
    {
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public string SigningKey { get; set; }
        public TimeSpan expirytTime { get; set; }
    }
}
