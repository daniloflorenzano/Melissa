let nativePort = null;

function connectNative() {
    nativePort = browser.runtime.connectNative("com.yourcompany.meetrecorder");
    nativePort.onMessage.addListener((msg) => {
        console.log("Recebeu do app nativo:", msg);
    });
    nativePort.onDisconnect.addListener(() => {
        console.log("Conexão com app nativo encerrada");
        nativePort = null;
    });
}

// Conectar assim que a extensão inicia
//connectNative();

browser.runtime.onMessage.addListener(async (message, sender) => {
    if (message.action === "startRecording") {
        // Mandar comando para app .NET iniciar gravação
        if (nativePort) {
            nativePort.postMessage({ command: "startRecording" });
            console.log("Pedido para iniciar gravação enviado");
        } else {
            console.error("App nativo não conectado");
        }
    }
});

// Detecta se uma aba do Meet abriu e notifica a popup para perguntar gravação
browser.tabs.onUpdated.addListener((tabId, changeInfo, tab) => {

    let isGoogleMeet = tab.url && tab.url.includes("meet.google.com");
    let isLoadingComplete = changeInfo.status === "complete";

    if (!isGoogleMeet || !isLoadingComplete) {
        return;
    }

    try {
        console.log("Pagina do meet carregada:", tab.url);
        browser.browserAction.enable(tabId);
        browser.tabs.sendMessage(tabId, { action: "meetingStarted" });
        console.log("Mensagem enviada");
    }
    catch (error) {
        console.error("Erro ao enviar mensagem para a aba do Meet:", error);
    }
});
