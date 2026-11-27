namespace Pc.Repositorio.Comum
{
    public static class SqlLikeHelper
    {
        public static string EscapeLike(string? valor) =>
            string.IsNullOrEmpty(valor)
                ? string.Empty
                : valor.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
    }
}
