/**
 * Console autoscroll functionality
 * Scrolls a container element to the bottom to show latest messages
 */

export function scrollToBottom(element) {
    if (element) {
        // Use a small timeout to ensure the DOM has been updated
        setTimeout(() => {
            element.scrollTop = element.scrollHeight;
            //log to console for debugging
            console.log("Scrolled to bottom of element");
        }, 0);
    }
}
