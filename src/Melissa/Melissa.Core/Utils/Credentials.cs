namespace Melissa.Core.Utils;

public static class Credentials
{
    /// <summary>
    /// Busca as credenciais de email e app password.
    /// </summary>
    public static class EmailCredsLoader
    {
        public static (string Email, string AppPassword) Load()
        {
            var credentialsPath = GetCredentials();
            
            if (!File.Exists(credentialsPath))
                throw new FileNotFoundException($"Arquivo de credenciais não encontrado em: {credentialsPath}");

            var dict = File.ReadAllLines(credentialsPath)
                .Where(l => !string.IsNullOrWhiteSpace(l) && l.Contains('='))
                .Select(l => l.Split('=', 2))
                .ToDictionary(a => a[0].Trim(), a => a[1].Trim(), StringComparer.OrdinalIgnoreCase);

            if (!dict.TryGetValue("email", out var email) || !dict.TryGetValue("app_password", out var appPassword))
                throw new InvalidOperationException("Arquivo de credenciais inválido. Esperado chaves: email, app_password.");

            return (email, appPassword);
        }
        
        private static string GetCredentials()
        {
            var folder = Environment.CurrentDirectory;
            var projectRoot = Directory.GetParent(folder)?.Parent?.Parent?.Parent?.FullName;
            var targetPath = Path.Combine(projectRoot!, "Melissa.Core", "Utils");
            return Path.Combine(targetPath, "email_credentials.txt");
        }
    }
}