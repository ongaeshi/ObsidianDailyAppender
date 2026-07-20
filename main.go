package main

import (
	"fmt"
	"os/exec"
	"strings"
	"time"

	"github.com/charmbracelet/huh"
	"github.com/charmbracelet/lipgloss"
)

func main() {
	// Print Header
	headerStyle := lipgloss.NewStyle().Foreground(lipgloss.Color("12")).Bold(true)
	subHeaderStyle := lipgloss.NewStyle().Foreground(lipgloss.Color("240"))

	fmt.Println(headerStyle.Render("--- Obsidian Daily Appender (Ctrl+C で終了) ---"))
	fmt.Println(subHeaderStyle.Render("Enter: 改行, Tab: 送信ボタンへ移動, 確定: SubmitでEnter"))
	fmt.Println()

	for {
		var text string

		form := huh.NewForm(
			huh.NewGroup(
				huh.NewText().
					Title("内容を入力してください (exitで終了):").
					Value(&text).
					Lines(5),
			),
		)

		err := form.Run()
		if err != nil {
			// user cancelled or ctrl+c
			break
		}

		text = strings.TrimSpace(text)
		if strings.ToLower(text) == "exit" {
			break
		}

		if text != "" {
			postToObsidian(text)
			fmt.Println()
		}
	}
}

func postToObsidian(arg string) {
	var text string
	if strings.HasPrefix(arg, "- ") || strings.HasPrefix(arg, "1. ") {
		text = arg
	} else if strings.HasPrefix(arg, "| ") {
		text = arg[2:]
	} else {
		now := time.Now().Format("15:04")
		text = fmt.Sprintf("\n---\n%s %s", now, arg)
	}

	cmd := exec.Command("obsidian.com", "daily:append", fmt.Sprintf("content=%s", text))
	
	out, err := cmd.CombinedOutput()
	
	if err != nil {
		errorStyle := lipgloss.NewStyle().Foreground(lipgloss.Color("9")).Bold(true)
		fmt.Println(errorStyle.Render("エラーが発生しました:"), err)
		if len(out) > 0 {
			fmt.Println(string(out))
		}
	} else {
		successStyle := lipgloss.NewStyle().Foreground(lipgloss.Color("14")).Bold(true)
		panelStyle := lipgloss.NewStyle().
			Border(lipgloss.RoundedBorder()).
			BorderForeground(lipgloss.Color("14")).
			Padding(0, 1)
			
		fmt.Println(successStyle.Render(">> 追記しました:"))
		fmt.Println(panelStyle.Render(arg))
	}
}
