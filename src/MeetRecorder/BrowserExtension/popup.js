// Função para mostrar o prompt de gravação
function showRecordingPrompt() {
    const prompt = document.getElementById("recordingPrompt");
    if (prompt) prompt.style.display = "block";
}

// Função para esconder o prompt
function hideRecordingPrompt() {
    const prompt = document.getElementById("recordingPrompt");
    if (prompt) prompt.style.display = "none";
}

// Escuta mensagens do background
browser.runtime.onMessage.addListener((message) => {
    console.log("Mensagem recebida no popup:", message);
    if (message.action === "meetingStarted") {
        showRecordingPrompt();
    }
});

// Ao abrir o popup, pede o estado atual da reunião
(async () => {
    const response = await browser.runtime.sendMessage({ action: "getMeetingStatus" });
    if (response && response.meetingActive) {
        showRecordingPrompt();
    } else {
        hideRecordingPrompt();
    }
})();

// Botão iniciar gravação
document.getElementById("startBtn").addEventListener("click", async () => {
    await browser.runtime.sendMessage({ action: "startRecording" });
    window.close();
});

// Botão cancelar
document.getElementById("cancelBtn").addEventListener("click", () => {
    window.close();
});
