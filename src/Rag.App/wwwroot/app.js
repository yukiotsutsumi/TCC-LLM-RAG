window.loginUser = async function (username, password, returnUrl) {
    try {
        const response = await fetch('/account/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ username, password })
        });

        if (!response.ok) return false;

        const data = await response.json();
        if (!data.success) return false;

        window.location.href = returnUrl || '/home';
        return true;
    } catch {
        return false;
    }
};

window.logoutUser = async function () {
    console.log(">>> logoutUser chamado");
    try {
        const response = await fetch('/account/logout', {
            method: 'POST',
            credentials: 'include'
        });
        console.log(">>> logout response status:", response.status);
    } catch (e) {
        console.error(">>> logout fetch erro:", e);
    } finally {
        console.log(">>> redirecionando para /login");
        window.location.href = '/login';
    }
};

window.checkAuth = async function () {
    try {
        const response = await fetch('/account/check-auth', {
            credentials: 'include'
        });
        return response.ok;
    } catch {
        return false;
    }
};

window._chatMessages = [];

window.pushChatMessage = function (role, content) {
    window._chatMessages.push({ role, content });
};

window.clearChatMessages = function () {
    window._chatMessages = [];
};

window.exportChat = function (filename) {
    const lines = [`RAG.chat — Conversa exportada`, `Data: ${new Date().toLocaleString('pt-BR')}`, ''];
    for (const m of window._chatMessages) {
        lines.push(`${m.role === 'user' ? 'Você' : 'Assistente'}: ${m.content}`);
    }

    const blob = new Blob([lines.join('\n')], { type: 'text/plain;charset=utf-8' });
    const url = URL.createObjectURL(blob);

    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.style.display = 'none';
    document.body.appendChild(a);
    a.click();

    setTimeout(() => {
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    }, 100);
};