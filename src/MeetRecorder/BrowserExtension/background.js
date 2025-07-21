let nativePort = null;

function getNativePort() {
    if (!nativePort) {
        nativePort = browser.runtime.connectNative("com.yourcompany.meetrecorder");
        console.log("Porta nativa conectada: ", nativePort);

        nativePort.onDisconnect.addListener(() => {
            console.log("Porta desconectada");
            nativePort = null;
        });
        nativePort.onMessage.addListener((msg) =>
            console.log("Host respondeu:", msg)
        );
    }
    return nativePort;
}

async function sendToHost(cmd) {
    const port = getNativePort();
    if (port) {
        port.postMessage({command: cmd});
    } else {
        console.error("App nativo não conectado");
    }
}

let isRecording = false;
browser.runtime.onMessage.addListener(async (message, sender) => {
    if (message.action === "startRecording") {
        // Mandar comando para app .NET iniciar gravação
        console.log("Mensagem recebida para iniciar gravação:", message);

        await sendToHost("startRecording")
        console.log("Pedido para iniciar gravação enviado");
        isRecording = true;
    }

    if (message.action === "meetEnded") {
        console.log("Mensagem recebida para parar gravação:", message);

        console.log("Pedido para parar gravação enviado");
        await sendToHost("stopRecording");
        isRecording = false;
    }
});

let recordingTabId = null;

// Detecta se uma aba do Meet abriu e notifica a popup para perguntar gravação
browser.tabs.onUpdated.addListener((tabId, changeInfo, tab) => {

    let isGoogleMeet = tab.url && tab.url.includes("meet.google.com");
    let isLoadingComplete = changeInfo.status === "complete";

    if (!isGoogleMeet) {
        return;
    }

    if (isLoadingComplete) {
        // Envia mensagem para a aba do Meet para iniciar monitoramento
        browser.tabs.sendMessage(tabId, {action: "meetingStarted"})
            .catch(error => console.error("Erro ao enviar mensagem para a aba do Meet:", error));

        recordingTabId = tabId;
    }
});

browser.tabs.onRemoved.addListener(async (tabId, removeInfo) => {

    if (tabId !== recordingTabId || !isRecording) {
        return;
    }

    console.log("Aba do Meet foi fechada. Parando gravação...");
    await sendToHost("stopRecording");
    isRecording = false;
    recordingTabId = null;
});

