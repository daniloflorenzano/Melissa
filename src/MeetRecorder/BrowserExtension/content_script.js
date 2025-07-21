function isInMeetCall() {
    return window.location.hostname === "meet.google.com";
}

// Escuta mensagens do background
browser.runtime.onMessage.addListener((message) => {
    console.log("Mensagem recebida no content_script:", message);
    if (message.action === "meetingStarted") {
        showRecordingPrompt();
    }
});

let lastSpeaker = null;

function monitorSpeaking() {
    const observer = new MutationObserver(() => {
        const speakingElem = document.querySelector('[data-self-name][aria-label*="is speaking"]');

        if (speakingElem) {
            const name = speakingElem.getAttribute("data-self-name");

            if (name && name !== lastSpeaker) {
                lastSpeaker = name;
                browser.runtime.sendMessage({
                    action: "speaking",
                    name,
                    timestamp: Date.now()
                });
            }
        }
    });

    observer.observe(document.body, { childList: true, subtree: true });
}

// if (isInMeetCall()) {
//     browser.runtime.sendMessage({ action: "meetStarted" });
//     //monitorSpeaking();
//
//     window.addEventListener("beforeunload", () => {
//         browser.runtime.sendMessage({ action: "meetEnded" });
//     });
// }


function showRecordingPrompt() {
    if (document.getElementById("recordPromptContainer")) return;

    const container = document.createElement("div");
    container.id = "recordPromptContainer";

    container.innerHTML = `
        <div class="record-prompt-backdrop">
            <div class="record-prompt-card">
                <div class="record-prompt-title">Iniciar gravação da reunião?</div>
                <div class="record-prompt-actions">
                    <button id="startRecBtn" class="record-btn record-start">Iniciar</button>
                    <button id="cancelRecBtn" class="record-btn record-cancel">Cancelar</button>
                </div>
            </div>
        </div>
        <style>
            .record-prompt-backdrop {
                position: fixed;
                top: 0; left: 0; right: 0; bottom: 0;
                background: rgba(0, 0, 0, 0.4);
                display: flex;
                align-items: center;
                justify-content: center;
                z-index: 9999;
            }

            .record-prompt-card {
                background: white;
                border-radius: 8px;
                box-shadow: 0 4px 12px rgba(0, 0, 0, 0.25);
                padding: 24px 32px;
                max-width: 400px;
                width: 100%;
                font-family: "Roboto", sans-serif;
                animation: fadeIn 0.3s ease-out;
            }

            .record-prompt-title {
                font-size: 18px;
                font-weight: 500;
                margin-bottom: 20px;
                text-align: center;
            }

            .record-prompt-actions {
                display: flex;
                justify-content: flex-end;
                gap: 10px;
            }

            .record-btn {
                padding: 8px 16px;
                border: none;
                border-radius: 4px;
                cursor: pointer;
                font-size: 14px;
                transition: background-color 0.2s ease;
            }

            .record-start {
                background-color: #1a73e8;
                color: white;
            }

            .record-start:hover {
                background-color: #1669c1;
            }

            .record-cancel {
                background-color: #e0e0e0;
                color: #333;
            }

            .record-cancel:hover {
                background-color: #d5d5d5;
            }

            @keyframes fadeIn {
                from { opacity: 0; transform: translateY(10px); }
                to { opacity: 1; transform: translateY(0); }
            }
        </style>
    `;

    document.body.appendChild(container);

    document.getElementById("startRecBtn").addEventListener("click", () => {
        browser.runtime.sendMessage({ action: "startRecording" });
        container.remove();
    });

    document.getElementById("cancelRecBtn").addEventListener("click", () => {
        container.remove();
    });
}
