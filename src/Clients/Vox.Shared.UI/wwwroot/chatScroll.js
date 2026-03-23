export function getScrollTop(selector) {
    const el = document.querySelector(selector);
    return el ? el.scrollTop : -1;
}

export function scrollToBottom(selector) {
    setTimeout(() => {
        const el = document.querySelector(selector);
        if (el) {
            el.scrollTop = el.scrollHeight;
        }
    }, 50);
}
