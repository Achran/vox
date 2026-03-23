export function getScrollTop(element) {
    return element ? element.scrollTop : -1;
}

export function scrollToBottom(element) {
    if (element) {
        setTimeout(() => {
            element.scrollTop = element.scrollHeight;
        }, 50);
    }
}

export function getScrollHeight(element) {
    return element ? element.scrollHeight : 0;
}

export function setScrollTop(element, value) {
    if (element) {
        element.scrollTop = value;
    }
}
