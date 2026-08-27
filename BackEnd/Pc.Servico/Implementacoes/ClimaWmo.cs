namespace Pc.Servico.Implementacoes
{
    /// <summary>
    /// Códigos WMO (Open-Meteo) → descrição PT-BR e ícone estável para o app.
    /// </summary>
    public static class ClimaWmo
    {
        public static (string Descricao, string Icone) Interpretar(int? codigo)
        {
            return codigo switch
            {
                0 => ("Céu limpo", "clear"),
                1 => ("Predominantemente claro", "clear"),
                2 => ("Parcialmente nublado", "partly-cloudy"),
                3 => ("Nublado", "cloudy"),
                45 or 48 => ("Neblina", "fog"),
                51 or 53 or 55 or 56 or 57 => ("Garoa", "drizzle"),
                61 or 63 or 65 or 66 or 67 => ("Chuva", "rain"),
                71 or 73 or 75 or 77 => ("Neve", "snow"),
                80 or 81 or 82 => ("Pancadas de chuva", "rain"),
                85 or 86 => ("Pancadas de neve", "snow"),
                95 or 96 or 99 => ("Tempestade", "thunder"),
                _ => ("Condição indefinida", "cloudy")
            };
        }

        public static string? UfBrasil(string? admin1)
        {
            if (string.IsNullOrWhiteSpace(admin1))
                return admin1;

            var nome = admin1.Trim();
            if (nome.Length == 2)
                return nome.ToUpperInvariant();

            return nome.ToLowerInvariant() switch
            {
                "acre" => "AC",
                "alagoas" => "AL",
                "amapá" or "amapa" => "AP",
                "amazonas" => "AM",
                "bahia" => "BA",
                "ceará" or "ceara" => "CE",
                "distrito federal" => "DF",
                "espírito santo" or "espirito santo" => "ES",
                "goiás" or "goias" => "GO",
                "maranhão" or "maranhao" => "MA",
                "mato grosso" => "MT",
                "mato grosso do sul" => "MS",
                "minas gerais" => "MG",
                "pará" or "para" => "PA",
                "paraíba" or "paraiba" => "PB",
                "paraná" or "parana" => "PR",
                "pernambuco" => "PE",
                "piauí" or "piaui" => "PI",
                "rio de janeiro" => "RJ",
                "rio grande do norte" => "RN",
                "rio grande do sul" => "RS",
                "rondônia" or "rondonia" => "RO",
                "roraima" => "RR",
                "santa catarina" => "SC",
                "são paulo" or "sao paulo" => "SP",
                "sergipe" => "SE",
                "tocantins" => "TO",
                _ => nome
            };
        }
    }
}
