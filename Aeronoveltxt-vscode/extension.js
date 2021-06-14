const vscode = require('vscode');

function activate(context) {

  context.subscriptions.push(vscode.commands.registerCommand('aeronoveltxt.search', () => {
    const editor = vscode.window.activeTextEditor;
    const text = editor.document.getText(editor.selection);
    vscode.env.openExternal(vscode.Uri.parse('https://www.google.com/search?q=' + text));
  }));
  context.subscriptions.push(vscode.commands.registerCommand('aeronoveltxt.searchp1', () => {
    const editor = vscode.window.activeTextEditor;
    const text = editor.document.getText(editor.selection);
    vscode.env.openExternal(vscode.Uri.parse('https://www.google.com/search?q=' + text + ' 意思'));
  }));
  context.subscriptions.push(vscode.commands.registerCommand('aeronoveltxt.weblio', () => {
    const editor = vscode.window.activeTextEditor;
    const text = editor.document.getText(editor.selection);
    vscode.env.openExternal(vscode.Uri.parse('https://www.weblio.jp/content/' + text));
  }));
  context.subscriptions.push(vscode.commands.registerTextEditorCommand('aeronoveltxt.onDownArrow', (editor, edit, args) => {
    vscode.commands.executeCommand('cursorMove', {
      to: "down",
      by: "wrappedLine"
    }).then(() => {
      const position = editor.selection.active;
      let line = position.line;
      let testRange = new vscode.Range(position.with(line, 0), position.with(line, 2));
      let testText = editor.document.getText(testRange);
      while (testText == "##" && line < editor.document.lineCount) {
        line++
        testRange = new vscode.Range(position.with(line, 0), position.with(line, 2));
        testText = editor.document.getText(testRange);
      }
      if (line != position.line) {
        let pos = position.with(line, position.character);
        switch (testText) {
          case "「」":
          case "『』":
          case "（）":
          case "〔〕":
            let raw = editor.document.getText(new vscode.Range(position.with(line - 1, 2), position.with(line - 1, 4)));
            if (raw[0] == testText[0]) { pos = pos.with(pos.line, 1); }
            break;
        }
        let sel = new vscode.Selection(pos, pos);
        editor.selection = sel;
        editor.revealRange(sel.with(), 1);//1=TextEditorRevealType.InCenter
      }
    });

  }));
}
exports.activate = activate;

function deactivate() {
}
exports.deactivate = deactivate;