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

window.scrollChatToBottom = function (elementId) {
    try {
        var el = document.getElementById(elementId);
        if (el) el.scrollTop = el.scrollHeight;
    } catch (e) { }
};