namespace APIPartNet.DTOs.Auth
{
    public class CredentialsDTO
    {
        public string Token { get; set; }
        public DateTime Expires { get; set; }

        public CredentialsDTO(DateTime expires, string token)
        {
            Token = token;
            Expires = expires;
        }
    }
}
