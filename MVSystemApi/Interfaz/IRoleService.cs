namespace MVSystemApi.Interfaz
{
    public interface IRoleService
    {
        Task<List<string>> GetPermisos();
    }
}
