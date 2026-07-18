htmx.on('htmx:beforeSwap', async function(evt) {
    if (evt.detail.xhr.status === 401 || evt.detail.xhr.status === 403) {
        if (!await try_refresh_token()) {
            window.location.replace('/auth/login');
        } else {
            alert("Обновление авторизации! Попробуйте перейти снова.");
        }
    }
});

async function try_refresh_token() {
    try {
        const res = await fetch('/auth/refresh');
        return res.status === 200;
    } catch (e) {
        alert("Произошла ошибка во время запроса!");
        console.error(e);
        return false;
    }
}

function format_file_size(bytes) {
    if (bytes < 1024) return bytes + ' Б';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' КБ';
    return (bytes / (1024 * 1024)).toFixed(1) + ' МБ';
}
