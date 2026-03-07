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