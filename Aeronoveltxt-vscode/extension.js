const vscode = require('vscode');

function activate(context) {

  context.subscriptions.push(vscode.commands.registerCommand('aeronoveltxt.search', () => {
    const editor = vscode.window.activeTextEditor;
    const text = editor.document.getText(editor.selection);
    vscode.env.openExternal(vscode.Uri.parse('https://www.google.com/search?q='+text));
  }));
  context.subscriptions.push(vscode.commands.registerCommand('aeronoveltxt.searchp1', () => {
    const editor = vscode.window.activeTextEditor;
    const text = editor.document.getText(editor.selection);
    vscode.env.openExternal(vscode.Uri.parse('https://www.google.com/search?q='+text+' 意思'));
  }));
  context.subscriptions.push(vscode.commands.registerCommand('aeronoveltxt.weblio', () => {
    const editor = vscode.window.activeTextEditor;
    const text = editor.document.getText(editor.selection);
    vscode.env.openExternal(vscode.Uri.parse('https://www.weblio.jp/content/'+text));
  }));
}
exports.activate = activate;

function deactivate() {
}
exports.deactivate = deactivate;