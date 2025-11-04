// Numeric input helper for serial number field
export function initializeNumericInput(elementId) {
    const element = document.getElementById(elementId);
    if (!element) {
        console.error(`Element with id '${elementId}' not found`);
        return;
    }

    element.addEventListener('keydown', (e) => {
        // Allow: Ctrl+V, Ctrl+C, Ctrl+A, Ctrl+X (paste, copy, select all, cut)
        if (e.ctrlKey && ['v', 'c', 'a', 'x'].includes(e.key.toLowerCase())) {
            return; // Allow
        }

        // Allow: Backspace, Delete, Tab, Enter, Arrows
        if (['Backspace', 'Delete', 'Tab', 'Enter', 'ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown'].includes(e.key)) {
            return; // Allow
        }

        // Allow: digits 0-9
        if (e.key.length === 1 && /[0-9]/.test(e.key)) {
            return; // Allow
        }

        // Block everything else
        e.preventDefault();
    });

    element.addEventListener('input', (e) => {
        // Filter the value to only numeric characters
        const filtered = e.target.value.replace(/[^0-9]/g, '');
        if (e.target.value !== filtered) {
            e.target.value = filtered;
            // Trigger change event so Blazor picks up the filtered value
            e.target.dispatchEvent(new Event('change', { bubbles: true }));
        }
    });

    console.log(`Numeric input initialized for '${elementId}'`);
}

export function disposeNumericInput(elementId) {
    // Cleanup if needed (event listeners are automatically removed when element is removed)
    console.log(`Numeric input disposed for '${elementId}'`);
}

export function focusElement(elementId) {
    const element = document.getElementById(elementId);
    if (!element) {
        console.warn(`Unable to focus element '${elementId}'`);
        return;
    }

    element.focus({ preventScroll: false });

    if (typeof element.select === 'function') {
        element.select();
    }
}
