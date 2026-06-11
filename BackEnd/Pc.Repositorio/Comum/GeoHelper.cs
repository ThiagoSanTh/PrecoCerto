namespace Pc.Repositorio.Comum
{
    internal static class GeoHelper
    {
        public static decimal CalcularDistanciaKm(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
        {
            const decimal raioTerraKm = 6371m;

            var dLat = (lat2 - lat1) * (decimal)Math.PI / 180m;
            var dLon = (lon2 - lon1) * (decimal)Math.PI / 180m;
            var a = (decimal)Math.Sin((double)dLat / 2) * (decimal)Math.Sin((double)dLat / 2) +
                    (decimal)Math.Cos((double)lat1 * Math.PI / 180) * (decimal)Math.Cos((double)lat2 * Math.PI / 180) *
                    (decimal)Math.Sin((double)dLon / 2) * (decimal)Math.Sin((double)dLon / 2);
            var c = 2m * (decimal)Math.Atan2(Math.Sqrt((double)a), Math.Sqrt((double)(1 - a)));

            return raioTerraKm * c;
        }
    }
}
