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

AnsiConsole.MarkupLine("[bold blue]--- Obsidian 追記モード (exit または Ctrl+C で終了) ---[/]");
AnsiConsole.MarkupLine("[grey]Shift+Enter: 改行, Enter: 送信[/]");
AnsiConsole.MarkupLine("[grey]↑/↓/←/→キー: カーソル移動, Backspace/Delete: 編集[/]");
AnsiConsole.WriteLine();

while (true)
{
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
    var lines = new List<StringBuilder> { new StringBuilder() };
    int cursorX = 0; // index in the string
    int cursorY = 0; // line index
    
    if (Console.CursorTop >= Console.BufferHeight - 1)
    {
        Console.WriteLine();
        Console.SetCursorPosition(0, Console.CursorTop - 1);
    }
    int startTop = Console.CursorTop;

    int GetVisualWidth(string text, int length)
    {
        int width = 0;
        for (int i = 0; i < length; i++)
        {
            char c = text[i];
            if (c < 0x0100) 
            {
                width += 1;
            }
            else if ((c >= 0x1100 && c <= 0x115F) || 
                     (c >= 0x2E80 && c <= 0xA4CF && c != 0x303F) || 
                     (c >= 0xAC00 && c <= 0xD7A3) || 
                     (c >= 0xF900 && c <= 0xFAFF) || 
                     (c >= 0xFE10 && c <= 0xFE19) || 
                     (c >= 0xFE30 && c <= 0xFE6F) || 
                     (c >= 0xFF00 && c <= 0xFF60) || 
                     (c >= 0xFFE0 && c <= 0xFFE6))
            {
                width += 2;
            }
            else
            {
                width += 1; 
            }
        }
        return width;
    }

    void RedrawLine(int lineIndex)
    {
        int top = startTop + lineIndex;
        if (top < 0 || top >= Console.BufferHeight) return;

        Console.SetCursorPosition(0, top);
        Console.Write(new string(' ', Console.WindowWidth - 1));
        Console.SetCursorPosition(0, top);
        AnsiConsole.Markup("[green]>[/] ");
        Console.Write(lines[lineIndex].ToString());
    }

    void RedrawAll(int fromLine = 0)
    {
        Console.CursorVisible = false;
        for (int i = fromLine; i < lines.Count; i++)
        {
            RedrawLine(i);
        }
        
        int clearTop = startTop + lines.Count;
        if (clearTop >= 0 && clearTop < Console.BufferHeight)
        {
            Console.SetCursorPosition(0, clearTop);
            Console.Write(new string(' ', Console.WindowWidth - 1));
        }
        
        SetCursor();
        Console.CursorVisible = true;
    }

    void SetCursor()
    {
        int visualX = GetVisualWidth(lines[cursorY].ToString(), cursorX);
        int top = startTop + cursorY;
        if (top < 0) top = 0;
        if (top >= Console.BufferHeight) top = Console.BufferHeight - 1;
        Console.SetCursorPosition(visualX + 2, top);
    }

    RedrawAll();

    while (true)
    {
        ConsoleKeyInfo key;
        try { key = Console.ReadKey(intercept: true); }
        catch (InvalidOperationException) { return null; }

        if (key.Modifiers.HasFlag(ConsoleModifiers.Control) && key.Key == ConsoleKey.C)
        {
            Console.WriteLine();
            return null; 
        }
        if (key.Modifiers.HasFlag(ConsoleModifiers.Control) && key.Key == ConsoleKey.Q)
        {
            Console.WriteLine();
            return null; 
        }

        if (key.Key == ConsoleKey.Enter)
        {
            if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
            {
                var currentLine = lines[cursorY];
                var nextLine = new StringBuilder(currentLine.ToString(cursorX, currentLine.Length - cursorX));
                currentLine.Length = cursorX;
                
                lines.Insert(cursorY + 1, nextLine);
                cursorY++;
                cursorX = 0;
                
                if (startTop + lines.Count >= Console.BufferHeight)
                {
                    Console.SetCursorPosition(0, Console.BufferHeight - 1);
                    Console.WriteLine();
                    startTop--; 
                }
                RedrawAll(cursorY - 1);
            }
            else
            {
                Console.SetCursorPosition(0, startTop + lines.Count);
                Console.WriteLine();
                break;
            }
        }
        else if (key.Key == ConsoleKey.LeftArrow)
        {
            if (cursorX > 0)
            {
                cursorX--;
                SetCursor();
            }
            else if (cursorY > 0)
            {
                cursorY--;
                cursorX = lines[cursorY].Length;
                SetCursor();
            }
        }
        else if (key.Key == ConsoleKey.RightArrow)
        {
            if (cursorX < lines[cursorY].Length)
            {
                cursorX++;
                SetCursor();
            }
            else if (cursorY < lines.Count - 1)
            {
                cursorY++;
                cursorX = 0;
                SetCursor();
            }
        }
        else if (key.Key == ConsoleKey.UpArrow)
        {
            if (cursorY > 0)
            {
                cursorY--;
                cursorX = Math.Min(cursorX, lines[cursorY].Length);
                SetCursor();
            }
            else
            {
                cursorX = 0;
                SetCursor();
            }
        }
        else if (key.Key == ConsoleKey.DownArrow)
        {
            if (cursorY < lines.Count - 1)
            {
                cursorY++;
                cursorX = Math.Min(cursorX, lines[cursorY].Length);
                SetCursor();
            }
            else
            {
                cursorX = lines[cursorY].Length;
                SetCursor();
            }
        }
        else if (key.Key == ConsoleKey.Backspace)
        {
            if (cursorX > 0)
            {
                lines[cursorY].Remove(cursorX - 1, 1);
                cursorX--;
                RedrawLine(cursorY);
                SetCursor();
            }
            else if (cursorY > 0)
            {
                var prevLine = lines[cursorY - 1];
                int oldLen = prevLine.Length;
                prevLine.Append(lines[cursorY]);
                lines.RemoveAt(cursorY);
                cursorY--;
                cursorX = oldLen;
                RedrawAll(cursorY);
            }
        }
        else if (key.Key == ConsoleKey.Delete)
        {
            if (cursorX < lines[cursorY].Length)
            {
                lines[cursorY].Remove(cursorX, 1);
                RedrawLine(cursorY);
                SetCursor();
            }
            else if (cursorY < lines.Count - 1)
            {
                lines[cursorY].Append(lines[cursorY + 1]);
                lines.RemoveAt(cursorY + 1);
                RedrawAll(cursorY);
            }
        }
        else if (key.Key == ConsoleKey.Home)
        {
            cursorX = 0;
            SetCursor();
        }
        else if (key.Key == ConsoleKey.End)
        {
            cursorX = lines[cursorY].Length;
            SetCursor();
        }
        else if (key.KeyChar >= 32 && key.KeyChar != 127) 
        {
            // Windows CMD might send keys we don't want (like escape codes if not intercepted properly)
            // But intercept=true handles it well.
            lines[cursorY].Insert(cursorX, key.KeyChar);
            cursorX++;
            RedrawLine(cursorY);
            SetCursor();
        }
    }
    
    return string.Join("\n", lines.Select(sb => sb.ToString()));
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
        text = $"---\n{time} {arg}\n";
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
