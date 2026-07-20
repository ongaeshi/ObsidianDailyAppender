using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Spectre.Console;

AnsiConsole.Write(
    new FigletText("oda")
        .LeftJustified()
        .Color(Color.Blue));

AnsiConsole.MarkupLine("[bold blue]--- Obsidian Daily Appender (exit または Ctrl+C で終了) ---[/]");
AnsiConsole.MarkupLine("[grey]Shift+Enter: 改行, Enter: 送信[/]");
AnsiConsole.MarkupLine("[grey]↑/↓/←/→キー: カーソル移動, Backspace/Delete: 削除[/]");
AnsiConsole.WriteLine();
var history = new List<string>();

while (true)
{
    var editor = new ObsidianDailyAppender.MultiLineEditor();
    editor.History = history;
    var input = editor.Read();
    
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
        history.Add(text);
        PostToObsidian(text);
        AnsiConsole.WriteLine();
    }
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
        text = $"\n---\n{time} {arg}";
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
