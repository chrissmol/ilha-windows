// Atualizar Tito.exe: fecha o Tito, puxa a main, compila e abre de novo (um "reboot" do Tito).
// Compilar: tools\CompilarAtualizador.cmd
using System;
using System.Diagnostics;
using System.IO;

static class AtualizarTito
{
    static int Run(string exe, string args, string dir)
    {
        var p = Process.Start(new ProcessStartInfo(exe, args) { WorkingDirectory = dir, UseShellExecute = false });
        p.WaitForExit();
        return p.ExitCode;
    }

    static int Main(string[] a)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        // shortcut: repo fixo em Documents\ilha-windows, ou passado como 1o argumento
        string repo = a.Length > 0 ? a[0] : Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ilha-windows");
        try
        {
            if (!Directory.Exists(Path.Combine(repo, ".git"))) throw new Exception("Repositorio nao encontrado: " + repo);

            Console.WriteLine("[1/4] Fechando o Tito...");
            foreach (var n in new[] { "TitoGlass", "Tito" })
                foreach (var p in Process.GetProcessesByName(n)) { try { p.Kill(); p.WaitForExit(5000); } catch { } }

            Console.WriteLine("[2/4] Baixando a ultima versao (main)...");
            if (Run("git", "pull --ff-only origin main", repo) != 0) throw new Exception("git pull falhou.");

            Console.WriteLine("[3/4] Compilando...");
            string ps = "-NoProfile -ExecutionPolicy Bypass -Command \"Get-ChildItem -LiteralPath '" + repo +
                "' -Recurse -File | Unblock-File -ErrorAction SilentlyContinue; & '" + Path.Combine(repo, "Compilar.ps1") + "'\"";
            if (Run("powershell", ps, repo) != 0) throw new Exception("A compilacao falhou.");

            Console.WriteLine("[4/4] Abrindo o Tito...");
            Process.Start(new ProcessStartInfo(Path.Combine(repo, "Tito.exe")) { WorkingDirectory = repo, UseShellExecute = true });
            Console.WriteLine("Pronto!");
            System.Threading.Thread.Sleep(2000);
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine();
            Console.WriteLine("ERRO: " + e.Message);
            Console.WriteLine("Pressione Enter para fechar.");
            Console.ReadLine();
            return 1;
        }
    }
}
