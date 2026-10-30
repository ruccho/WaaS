// The debug adapter runs in the Unity editor, and this extension only connects to it.
const vscode = require("vscode");

function activate(context) {
  context.subscriptions.push(
    vscode.debug.registerDebugAdapterDescriptorFactory("waas", {
      createDebugAdapterDescriptor(session) {
        const configuration = session.configuration;
        return new vscode.DebugAdapterServer(configuration.port ?? 4711, configuration.host ?? "127.0.0.1");
      },
    })
  );
}

function deactivate() {}

module.exports = { activate, deactivate };
