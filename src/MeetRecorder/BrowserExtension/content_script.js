function isInMeetCall() {
    return window.location.hostname === "meet.google.com";
}

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

if (isInMeetCall()) {
    browser.runtime.sendMessage({ action: "meetStarted" });
    monitorSpeaking();

    window.addEventListener("beforeunload", () => {
        browser.runtime.sendMessage({ action: "meetEnded" });
    });
}
