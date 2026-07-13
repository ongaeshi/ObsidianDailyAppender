using System;
using System.Diagnostics;
using System.Text;
using Spectre.Console;

AnsiConsole.Write(
    new FigletText("oda")
        .LeftJustified()
        .Color(Color.Blue));

AnsiConsole.MarkupLine("[bold blue]--- Obsidian 追記モード (exit または Ctrl+C で終了) ---[/]");
AnsiConsole.MarkupLine("[grey]Shift+Enter: 改行, Enter: 送信[/]");
AnsiConsole.WriteLine();

while (true)
{
    AnsiConsole.Markup("[green]>[/] ");
    var input = ReadMultiLineInput();
    
    if (input == null)
    {
        break; // Ctrl+C or EOF
    }

    var text = input.TrimEnd();
    if (text.ToLower() == "exit")
    {
        break;
    }

    if (!string.IsNullOrWhiteSpace(text))
    {
        PostToObsidian(text);
        AnsiConsole.WriteLine();
    }
}

string? ReadMultiLineInput()
{
    var sb = new StringBuilder();
    
    while (true)
    {
        ConsoleKeyInfo key;
        try
        {
            key = Console.ReadKey(intercept: true);
        }
        catch (InvalidOperationException)
        {
            // Input stream closed
            return null;
        }

        if (key.Modifiers.HasFlag(ConsoleModifiers.Control) && key.Key == ConsoleKey.C)
        {
            Console.WriteLine();
            return null; // Handle Ctrl+C gracefully
        }
        
        if (key.Modifiers.HasFlag(ConsoleModifiers.Control) && key.Key == ConsoleKey.Q)
        {
            Console.WriteLine();
            return null; // Handle Ctrl+Q gracefully
        }

        if (key.Key == ConsoleKey.Enter)
        {
            if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
            {
                sb.Append('\n');
                Console.WriteLine();
                AnsiConsole.Markup("[green]>[/] ");
            }
            else
            {
                Console.WriteLine();
                break;
            }
        }
        else if (key.Key == ConsoleKey.Backspace)
        {
            if (sb.Length > 0)
            {
                if (sb[sb.Length - 1] == '\n')
                {
                    sb.Length--;
                    Console.CursorTop--;
                    
                    int lastLineLen = 0;
                    for (int i = sb.Length - 1; i >= 0; i--)
                    {
                        if (sb[i] == '\n') break;
                        lastLineLen++;
                    }
                    Console.CursorLeft = lastLineLen + 2; 
                    Console.Write(" \b");
                }
                else
                {
                    sb.Length--;
                    Console.Write("\b \b");
                }
            }
        }
        else if (key.KeyChar != '\u0000') 
        {
            sb.Append(key.KeyChar);
            Console.Write(key.KeyChar);
        }
    }
    return sb.ToString();
}

void PostToObsidian(string arg)
{
    string text = "";
    if (arg.StartsWith("- "))
        text = arg;
    else if (arg.StartsWith("1. "))
        text = arg;
    else if (arg.StartsWith("| "))
        text = arg.Substring(2);
    else
    {
        string time = DateTime.Now.ToString("HH:mm");
        text = $"---\n{time} {arg}";
    }

    try
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "obsidian.com",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("daily:append");
        startInfo.ArgumentList.Add($"content={text}");

        using var process = Process.Start(startInfo);
        if (process != null)
        {
            process.WaitForExit();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            if (process.ExitCode == 0)
            {
                AnsiConsole.MarkupLine("[bold cyan]>> 追記しました:[/]");
                AnsiConsole.Write(new Panel(new Text(arg)).BorderColor(Color.Cyan));
            }
            else
            {
                AnsiConsole.MarkupLine("[bold red]エラーが発生しました:[/]");
                if (!string.IsNullOrWhiteSpace(output)) AnsiConsole.WriteLine(output);
                if (!string.IsNullOrWhiteSpace(error)) AnsiConsole.WriteLine(error);
            }
        }
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[bold red]実行エラー:[/] {ex.Message}");
    }
}
