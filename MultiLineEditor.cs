using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spectre.Console;

namespace ObsidianDailyAppender
{
    public class MultiLineEditor
    {
        private List<StringBuilder> _lines = new() { new StringBuilder() };
        private int _cursorRow = 0; // logical row
        private int _cursorCol = 0; // logical col (character index)
        private int _startTop = 0;
        private int _idealVisualX = -1;

        public string? Read()
        {
            if (Console.CursorTop >= Console.BufferHeight - 1)
            {
                Console.WriteLine();
                Console.SetCursorPosition(0, Console.CursorTop - 1);
            }
            _startTop = Console.CursorTop;

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
                        var currentLine = _lines[_cursorRow];
                        var nextLine = new StringBuilder(currentLine.ToString(_cursorCol, currentLine.Length - _cursorCol));
                        currentLine.Length = _cursorCol;

                        _lines.Insert(_cursorRow + 1, nextLine);
                        _cursorRow++;
                        _cursorCol = 0;

                        CheckScroll();
                        RedrawAll(_cursorRow - 1);
                    }
                    else
                    {
                        int totalLines = GetTotalPhysicalLines();
                        int endTop = _startTop + totalLines;
                        if (endTop >= Console.BufferHeight) endTop = Console.BufferHeight - 1;
                        Console.SetCursorPosition(0, endTop);
                        Console.WriteLine();
                        break;
                    }
                }
                else if (key.Key == ConsoleKey.LeftArrow)
                {
                    if (_cursorCol > 0)
                    {
                        _cursorCol--;
                        SetCursor();
                    }
                    else if (_cursorRow > 0)
                    {
                        _cursorRow--;
                        _cursorCol = _lines[_cursorRow].Length;
                        SetCursor();
                    }
                }
                else if (key.Key == ConsoleKey.RightArrow)
                {
                    if (_cursorCol < _lines[_cursorRow].Length)
                    {
                        _cursorCol++;
                        SetCursor();
                    }
                    else if (_cursorRow < _lines.Count - 1)
                    {
                        _cursorRow++;
                        _cursorCol = 0;
                        SetCursor();
                    }
                }
                else if (key.Key == ConsoleKey.UpArrow)
                {
                    MoveVertical(-1);
                }
                else if (key.Key == ConsoleKey.DownArrow)
                {
                    MoveVertical(1);
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (_cursorCol > 0)
                    {
                        _lines[_cursorRow].Remove(_cursorCol - 1, 1);
                        _cursorCol--;
                        RedrawAll(_cursorRow);
                    }
                    else if (_cursorRow > 0)
                    {
                        var prevLine = _lines[_cursorRow - 1];
                        int oldLen = prevLine.Length;
                        prevLine.Append(_lines[_cursorRow]);
                        _lines.RemoveAt(_cursorRow);
                        _cursorRow--;
                        _cursorCol = oldLen;
                        
                        RedrawAll(_cursorRow);
                    }
                }
                else if (key.Key == ConsoleKey.Delete)
                {
                    if (_cursorCol < _lines[_cursorRow].Length)
                    {
                        _lines[_cursorRow].Remove(_cursorCol, 1);
                        RedrawAll(_cursorRow);
                    }
                    else if (_cursorRow < _lines.Count - 1)
                    {
                        _lines[_cursorRow].Append(_lines[_cursorRow + 1]);
                        _lines.RemoveAt(_cursorRow + 1);
                        RedrawAll(_cursorRow);
                    }
                }
                else if (key.Key == ConsoleKey.Home)
                {
                    _cursorCol = 0;
                    SetCursor();
                }
                else if (key.Key == ConsoleKey.End)
                {
                    _cursorCol = _lines[_cursorRow].Length;
                    SetCursor();
                }
                else if (key.KeyChar >= 32 && key.KeyChar != 127)
                {
                    _lines[_cursorRow].Insert(_cursorCol, key.KeyChar);
                    _cursorCol++;
                    CheckScroll();
                    RedrawAll(_cursorRow);
                }
            // Reset ideal X on any other key
            if (key.Key != ConsoleKey.UpArrow && key.Key != ConsoleKey.DownArrow)
            {
                _idealVisualX = -1;
            }
            }

            return string.Join("\n", _lines.Select(sb => sb.ToString()));
        }

        private void MoveVertical(int deltaPhysicalRows)
        {
            GetCurrentPhysicalPos(out int currentPhysicalRowOffset, out int currentVisualX);
            if (_idealVisualX == -1) _idealVisualX = currentVisualX;

            if (deltaPhysicalRows < 0) // Up
            {
                if (currentPhysicalRowOffset > 0)
                {
                    _cursorCol = GetLogicalColFromPhysicalPos(_cursorRow, currentPhysicalRowOffset - 1, _idealVisualX);
                    SetCursor();
                }
                else if (_cursorRow > 0)
                {
                    _cursorRow--;
                    int prevLogicalPhysicalCount = GetPhysicalLines(_lines[_cursorRow].ToString()).Count;
                    _cursorCol = GetLogicalColFromPhysicalPos(_cursorRow, prevLogicalPhysicalCount - 1, _idealVisualX);
                    SetCursor();
                }
                else if (_cursorCol > 0)
                {
                    _cursorCol = 0;
                    SetCursor();
                }
            }
            else // Down
            {
                int currentLogicalPhysicalCount = GetPhysicalLines(_lines[_cursorRow].ToString()).Count;

                if (currentPhysicalRowOffset < currentLogicalPhysicalCount - 1)
                {
                    _cursorCol = GetLogicalColFromPhysicalPos(_cursorRow, currentPhysicalRowOffset + 1, _idealVisualX);
                    SetCursor();
                }
                else if (_cursorRow < _lines.Count - 1)
                {
                    _cursorRow++;
                    _cursorCol = GetLogicalColFromPhysicalPos(_cursorRow, 0, _idealVisualX);
                    SetCursor();
                }
                else if (_cursorCol < _lines[_cursorRow].Length)
                {
                    _cursorCol = _lines[_cursorRow].Length;
                    SetCursor();
                }
            }
        }

        private void GetCurrentPhysicalPos(out int physicalRowOffset, out int visualX)
        {
            string currentLogicalLine = _lines[_cursorRow].ToString();
            string textBeforeCursor = currentLogicalLine.Substring(0, _cursorCol);
            var pLinesBefore = GetPhysicalLines(textBeforeCursor);
            physicalRowOffset = pLinesBefore.Count - 1;
            if (physicalRowOffset < 0) physicalRowOffset = 0;
            
            visualX = 0;
            if (pLinesBefore.Count > 0)
            {
                visualX = GetVisualWidth(pLinesBefore.Last());
            }
            
            if (visualX + 2 >= Console.WindowWidth)
            {
                visualX = 0;
                physicalRowOffset++;
            }
        }

        private int GetLogicalColFromPhysicalPos(int logicalRow, int targetPhysicalRowOffset, int targetVisualX)
        {
            var pLines = GetPhysicalLines(_lines[logicalRow].ToString());
            if (pLines.Count == 0) return 0;
            if (targetPhysicalRowOffset >= pLines.Count) targetPhysicalRowOffset = pLines.Count - 1;
            
            int logicalCol = 0;
            for (int i = 0; i < targetPhysicalRowOffset; i++)
            {
                logicalCol += pLines[i].Length;
            }
            
            string targetLine = pLines[targetPhysicalRowOffset];
            int currentVisualX = 0;
            for (int i = 0; i < targetLine.Length; i++)
            {
                int charWidth = GetCharVisualWidth(targetLine[i]);
                if (currentVisualX + charWidth > targetVisualX)
                {
                    break;
                }
                currentVisualX += charWidth;
                logicalCol++;
            }
            
            return logicalCol;
        }

        private void CheckScroll()
        {
            int totalPhysical = GetTotalPhysicalLines();
            if (_startTop + totalPhysical >= Console.BufferHeight)
            {
                int diff = _startTop + totalPhysical - Console.BufferHeight + 1;
                for (int i = 0; i < diff; i++)
                {
                    Console.SetCursorPosition(0, Console.BufferHeight - 1);
                    Console.WriteLine();
                }
                _startTop -= diff;
                if (_startTop < 0) _startTop = 0;
            }
        }

        private int GetTotalPhysicalLines()
        {
            int sum = 0;
            for (int i = 0; i < _lines.Count; i++)
            {
                sum += GetPhysicalLines(_lines[i].ToString()).Count;
            }
            return sum;
        }

        private void RedrawAll(int fromLogicalLine = 0)
        {
            Console.CursorVisible = false;

            int physicalTop = _startTop;
            for (int i = 0; i < fromLogicalLine; i++)
            {
                physicalTop += GetPhysicalLines(_lines[i].ToString()).Count;
            }

            for (int i = fromLogicalLine; i < _lines.Count; i++)
            {
                var pLines = GetPhysicalLines(_lines[i].ToString());
                for (int p = 0; p < pLines.Count; p++)
                {
                    if (physicalTop >= Console.BufferHeight) break;
                    Console.SetCursorPosition(0, physicalTop);
                    
                    string prefix = (p == 0) ? "[green]>[/] " : "  ";
                    Console.Write(new string(' ', Console.WindowWidth - 1));
                    Console.SetCursorPosition(0, physicalTop);
                    AnsiConsole.Markup(prefix);
                    Console.Write(pLines[p]);
                    
                    physicalTop++;
                }
            }

            int clearTop = physicalTop;
            for (int i = 0; i < 5; i++)
            {
                if (clearTop + i >= Console.BufferHeight) break;
                Console.SetCursorPosition(0, clearTop + i);
                Console.Write(new string(' ', Console.WindowWidth - 1));
            }

            SetCursor();
            Console.CursorVisible = true;
        }

        private void SetCursor()
        {
            int physicalTop = _startTop;
            for (int i = 0; i < _cursorRow; i++)
            {
                physicalTop += GetPhysicalLines(_lines[i].ToString()).Count;
            }

            string currentLogicalLine = _lines[_cursorRow].ToString();
            string textBeforeCursor = currentLogicalLine.Substring(0, _cursorCol);
            
            var pLinesBefore = GetPhysicalLines(textBeforeCursor);
            
            int cursorPhysicalRowOffset = pLinesBefore.Count - 1;
            if (cursorPhysicalRowOffset < 0) cursorPhysicalRowOffset = 0;

            physicalTop += cursorPhysicalRowOffset;
            
            int cursorPhysicalCol = 2; // "> " or "  "
            if (pLinesBefore.Count > 0)
            {
                cursorPhysicalCol += GetVisualWidth(pLinesBefore.Last());
            }

            if (cursorPhysicalCol >= Console.WindowWidth)
            {
                cursorPhysicalCol = 2;
                physicalTop++;
            }

            if (physicalTop < 0) physicalTop = 0;
            if (physicalTop >= Console.BufferHeight) physicalTop = Console.BufferHeight - 1;

            Console.SetCursorPosition(cursorPhysicalCol, physicalTop);
        }

        private List<string> GetPhysicalLines(string text)
        {
            var physicalLines = new List<string>();
            int availableWidth = Console.WindowWidth - 3; // safe margin for scrolling/border issues (usually -1 but let's be safe with -3 to allow for 2 chars of prefix and 1 char space at end)
            // Actually, prefix is 2 chars. So available text width is WindowWidth - 3.
            if (availableWidth <= 0) availableWidth = 1;

            if (string.IsNullOrEmpty(text))
            {
                physicalLines.Add("");
                return physicalLines;
            }

            int currentWidth = 0;
            int startIndex = 0;

            for (int i = 0; i < text.Length; i++)
            {
                int charWidth = GetCharVisualWidth(text[i]);
                if (currentWidth + charWidth > availableWidth)
                {
                    if (i == startIndex)
                    {
                        physicalLines.Add(text.Substring(startIndex, 1));
                        startIndex = i + 1;
                        currentWidth = 0;
                    }
                    else
                    {
                        physicalLines.Add(text.Substring(startIndex, i - startIndex));
                        startIndex = i;
                        currentWidth = charWidth;
                    }
                }
                else
                {
                    currentWidth += charWidth;
                }
            }
            if (startIndex < text.Length)
            {
                physicalLines.Add(text.Substring(startIndex));
            }

            return physicalLines;
        }

        private int GetVisualWidth(string text)
        {
            int w = 0;
            foreach (char c in text) w += GetCharVisualWidth(c);
            return w;
        }

        private int GetCharVisualWidth(char c)
        {
            if (c < 0x0100) return 1;
            if ((c >= 0x1100 && c <= 0x115F) || 
                (c >= 0x2E80 && c <= 0xA4CF && c != 0x303F) || 
                (c >= 0xAC00 && c <= 0xD7A3) || 
                (c >= 0xF900 && c <= 0xFAFF) || 
                (c >= 0xFE10 && c <= 0xFE19) || 
                (c >= 0xFE30 && c <= 0xFE6F) || 
                (c >= 0xFF00 && c <= 0xFF60) || 
                (c >= 0xFFE0 && c <= 0xFFE6)) return 2;
            return 1;
        }
    }
}
