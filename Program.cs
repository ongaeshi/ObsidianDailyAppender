using System;
using System.Diagnostics;
using Terminal.Gui;

Application.Init();

var win = new Window("Obsidian Daily Appender (oda)")
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var logView = new TextView()
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill() - 5,
    ReadOnly = true,
    WordWrap = true
};
logView.Text = "--- Obsidian 追記モード (Ctrl+Q または exit で終了) ---\n";

var inputFrame = new FrameView("Input (Enter to Submit, Shift+Enter for Newline)")
{
    X = 0,
    Y = Pos.Bottom(logView),
    Width = Dim.Fill(),
    Height = 5
};

var inputView = new TextView()
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(),
    WordWrap = true
};

inputFrame.Add(inputView);
win.Add(logView, inputFrame);
Application.Top.Add(win);

inputView.KeyPress += (e) =>
{
    if (e.KeyEvent.Key == (Key.Enter | Key.ShiftMask))
    {
        inputView.ProcessKey(new KeyEvent(Key.Enter, new KeyModifiers()));
        e.Handled = true;
    }
    else if (e.KeyEvent.Key == Key.Enter)
    {
        Submit();
        e.Handled = true;
    }
};

void LogMessage(string message)
{
    logView.Text += message + "\n";
    logView.MoveEnd();
}

void Submit()
{
    var text = inputView.Text?.ToString()?.TrimEnd() ?? "";
    if (text.ToLower() == "exit")
    {
        Application.RequestStop();
        return;
    }
    if (!string.IsNullOrWhiteSpace(text))
    {
        PostToObsidian(text);
    }
    inputView.Text = "";
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
                LogMessage($">> 追記しました:\n{arg}");
            }
            else
            {
                LogMessage("エラーが発生しました:");
                if (!string.IsNullOrWhiteSpace(output)) LogMessage(output);
                if (!string.IsNullOrWhiteSpace(error)) LogMessage(error);
            }
        }
    }
    catch (Exception ex)
    {
        LogMessage($"実行エラー: {ex.Message}");
    }
}

Application.Run();
Application.Shutdown();
