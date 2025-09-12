namespace Melissa.Core.Utils;

public static class Credentials
{
    public const string CredentialsPath = @"C:\dev\Credenciais Email Melissa\email_credentials.txt";
    
    /// <summary>
    /// Busca as credenciais de email e app password.
    /// </summary>
    public static class EmailCredsLoader
    {
        public static (string Email, string AppPassword) Load()
        {
            if (!File.Exists(CredentialsPath))
                throw new FileNotFoundException($"Arquivo de credenciais não encontrado em: {CredentialsPath}");

            var dict = File.ReadAllLines(CredentialsPath)
                .Where(l => !string.IsNullOrWhiteSpace(l) && l.Contains('='))
                .Select(l => l.Split('=', 2))
                .ToDictionary(a => a[0].Trim(), a => a[1].Trim(), StringComparer.OrdinalIgnoreCase);

            if (!dict.TryGetValue("email", out var email) || !dict.TryGetValue("app_password", out var appPassword))
                throw new InvalidOperationException("Arquivo de credenciais inválido. Esperado chaves: email, app_password.");

            return (email, appPassword);
        }
    }
}