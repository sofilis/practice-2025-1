// AdvancedSimpleShell.cs (финальная версия)
// Исправленная версия с выходом из Wordle и улучшенной обработкой фоновых задач

using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Threading.Tasks;
using ReadLine = System.ReadLine; // Используем стороннюю библиотеку ReadLine (установить через NuGet)

namespace AdvancedSimpleShell
{
    class Program
    {
        static List<string> commandHistory = new();
        static List<string> tips = new()
        {
            "💡 help — твой друг!",
            "💬 Ты сильнее, чем думаешь.",
            "😂 Почему программисты не ходят в лес? Там много багов!",
            "💡 Используй 'cd ..' чтобы вернуться назад",
            "💬 Ошибки — это обучение.",
            "😂 Как зовут программиста в армии? Сержант-отладчик.",
            "💡 Попробуй 'http get https://catfact.ninja/fact'",
            "💬 Главное — не сдаваться!"
        };

        static Dictionary<int, Process> backgroundJobs = new();
        static int jobCounter = 1;

        static async Task Main(string[] args)
        {
            Console.CancelKeyPress += (sender, e) =>
            {
                Console.ResetColor();
                Console.WriteLine("\nПрерывание. Нажмите 'exit' для выхода.");
                e.Cancel = true;
            };

            Console.WriteLine("Добро пожаловать в AdvancedSimpleShell! Введите 'help' для списка команд.");

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"[{Directory.GetCurrentDirectory()}] > ");
                Console.ResetColor();
                string input = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(input)) continue;
                commandHistory.Add(input);

                string[] parts = input.Split(' ', 2);
                string command = parts[0];
                string arguments = parts.Length > 1 ? parts[1] : "";

                switch (command)
                {
                    case "exit":
                        Console.WriteLine("Пока!");
                        return;
                    case "help": ShowHelp(); break;
                    case "calc": RunCalc(arguments); break;
                    case "tips": ShowRandomTip(); break;
                    case "cd": ChangeDirectory(arguments); break;
                    case "pwd": Console.WriteLine(Directory.GetCurrentDirectory()); break;
                    case "jobs": ShowJobs(); break;
                    case "kill": KillJob(arguments); break;
                    case "http": await RunHttp(arguments); break;
                    case "wordle": RunWordle(); break;
                    default:
                        if (input.EndsWith("&"))
                        {
                            string fullCommand = input[..^1].Trim();
                            string[] bgParts = fullCommand.Split(' ', 2);
                            string bgCommand = bgParts[0];
                            string bgArgs = bgParts.Length > 1 ? bgParts[1] : "";
                            RunInBackground(bgCommand, bgArgs);
                        }
                        else
                        {
                            RunCommand(command, arguments);
                        }
                        break;
                }

                if (commandHistory.Count % 3 == 0)
                    ShowRandomTip();
            }
        }

        static void ShowHelp()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Команды:");
            Console.WriteLine("  help               - список команд");
            Console.WriteLine("  calc <выражение>   - калькулятор");
            Console.WriteLine("  cd <путь>          - сменить директорию");
            Console.WriteLine("  pwd                - текущая директория");
            Console.WriteLine("  http get <url>     - HTTP-запрос");
            Console.WriteLine("  jobs               - фоновые задачи");
            Console.WriteLine("  kill <номер>       - завершить фоновую задачу");
            Console.WriteLine("  wordle             - игра на русском (введите 'exit' для выхода)");
            Console.WriteLine("  <команда> &        - запуск любой команды в фоновом режиме");
            Console.WriteLine("  exit               - выход");
            Console.ResetColor();
        }

        static void RunCalc(string expr)
        {
            try
            {
                var result = new DataTable().Compute(expr, null);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Результат: {result}");
                Console.ResetColor();
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Ошибка в выражении");
                Console.ResetColor();
            }
        }

        static void ChangeDirectory(string path)
        {
            try
            {
                Directory.SetCurrentDirectory(path);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ошибка: {e.Message}");
                Console.ResetColor();
            }
        }

        static void RunCommand(string command, string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo(command, arguments)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                process.OutputDataReceived += (s, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ошибка: {e.Message}");
                Console.ResetColor();
            }
        }

        static void RunInBackground(string command, string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo(command, arguments)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var process = Process.Start(psi);
                backgroundJobs[jobCounter++] = process;
                Console.WriteLine($"Фоновая задача запущена [id: {jobCounter - 1}]");
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ошибка: {e.Message}");
                Console.ResetColor();
            }
        }

        static void ShowJobs()
        {
            foreach (var job in backgroundJobs)
            {
                Console.WriteLine($"[{job.Key}] {job.Value.StartInfo.FileName} {(job.Value.HasExited ? "(завершён)" : "(работает)")}");
            }
        }

        static void KillJob(string arg)
        {
            if (int.TryParse(arg, out int id) && backgroundJobs.ContainsKey(id))
            {
                try
                {
                    backgroundJobs[id].Kill();
                    Console.WriteLine($"Задача {id} завершена.");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Не удалось завершить: {e.Message}");
                }
            }
            else Console.WriteLine("Некорректный id задачи.");
        }

        static void ShowRandomTip()
        {
            var rand = new Random();
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(tips[rand.Next(tips.Count)]);
            Console.ResetColor();
        }

        static async Task RunHttp(string input)
        {
            if (!input.StartsWith("get "))
            {
                Console.WriteLine("Формат: http get <url>");
                return;
            }
            string url = input[4..];
            try
            {
                using HttpClient client = new();
                var response = await client.GetStringAsync(url);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(response);
                Console.ResetColor();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ошибка HTTP: {e.Message}");
                Console.ResetColor();
            }
        }

        static void RunWordle()
        {
            var words = new List<string> { "столб", "путь", "дышит", "сказа", "топор", "котик", "врата", "пламя" };
            string secret = words[new Random().Next(words.Count)].ToLower();
            Console.WriteLine("Игра Wordle на русском! Угадай слово из 5 букв. Введите 'exit' для выхода из игры.");
            for (int attempt = 1; attempt <= 6; attempt++)
            {
                Console.Write($"Попытка {attempt}/6: ");
                string guess = Console.ReadLine()?.ToLower() ?? "";
                if (guess == "exit")
                {
                    Console.WriteLine("Выход из Wordle.");
                    return;
                }
                if (guess.Length != 5)
                {
                    Console.WriteLine("Слово должно быть из 5 букв!");
                    attempt--;
                    continue;
                }

                for (int i = 0; i < 5; i++)
                {
                    if (guess[i] == secret[i]) Console.ForegroundColor = ConsoleColor.Green;
                    else if (secret.Contains(guess[i])) Console.ForegroundColor = ConsoleColor.Yellow;
                    else Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write(guess[i]);
                }
                Console.ResetColor();
                Console.WriteLine();

                if (guess == secret)
                {
                    Console.WriteLine("🎉 Поздравляем! Вы угадали слово!");
                    return;
                }
            }
            Console.WriteLine($"❌ Увы, слово было: {secret}");
        }
    }
}