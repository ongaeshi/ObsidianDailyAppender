import sys
import subprocess
from datetime import datetime
from prompt_toolkit import PromptSession
from prompt_toolkit.formatted_text import HTML
from prompt_toolkit.styles import Style
from prompt_toolkit.shortcuts import print_formatted_text
from prompt_toolkit.key_binding import KeyBindings

style = Style.from_dict({
    'header': 'ansiblue bold',
    'subheader': 'ansidarkgray',
    'success': 'ansicyan bold',
    'error': 'ansired bold',
})

def post_to_obsidian(arg: str):
    text = ""
    if arg.startswith("- ") or arg.startswith("1. "):
        text = arg
    elif arg.startswith("| "):
        text = arg[2:]
    else:
        now = datetime.now().strftime("%H:%M")
        text = f"\n---\n{now} {arg}"

    try:
        # Remove shell=True to avoid Windows cmd argument parsing issues
        result = subprocess.run(
            ["obsidian.com", "daily:append", f"content={text}"],
            capture_output=True,
            text=True
        )
        if result.returncode == 0:
            print_formatted_text(HTML('<success>&gt;&gt; 追記しました:</success>'), style=style)
            print(arg)
        else:
            print_formatted_text(HTML('<error>エラーが発生しました:</error>'), style=style)
            if result.stdout:
                print(result.stdout)
            if result.stderr:
                print(result.stderr)
    except Exception as e:
        print_formatted_text(HTML(f'<error>実行エラー:</error> {str(e)}'), style=style)

def main():
    print_formatted_text(HTML('<header>--- Obsidian Daily Appender (exit または Ctrl+C/Ctrl+D で終了) ---</header>'), style=style)
    print_formatted_text(HTML('<subheader>Enter: 改行, Ctrl+Enter: 送信</subheader>\n'), style=style)

    bindings = KeyBindings()
    # prompt_toolkit on Windows converts Ctrl+Enter into 'escape', 'enter'
    @bindings.add('escape', 'enter')
    def _(event):
        event.current_buffer.validate_and_handle()

    session = PromptSession()

    while True:
        try:
            # prompt_continuation allows us to show a character like '> ' for subsequent lines
            text = session.prompt(
                '内容を入力してください:\n',
                multiline=True,
                prompt_continuation=lambda width, line_number, is_soft_wrap: '> ',
                key_bindings=bindings
            )
        except KeyboardInterrupt:
            print()
            break
        except EOFError:
            print()
            break

        text = text.strip()
        if text.lower() == "exit":
            break

        if text:
            post_to_obsidian(text)
            print()

if __name__ == "__main__":
    main()
