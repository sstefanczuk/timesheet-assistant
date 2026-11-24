window.entryDialog = {
    open(dialog, receiver, dismissMethod = 'DismissAsync') {
        const previous = document.activeElement;
        dialog.addEventListener('cancel', event => {
            event.preventDefault();
            receiver.invokeMethodAsync(dismissMethod);
        });
        dialog.addEventListener('close', () => {
            if (previous && previous.isConnected) previous.focus();
        }, { once: true });
        dialog.showModal();
        (dialog.querySelector('input:not([disabled])')
            || dialog.querySelector('[autofocus]')
            || dialog.querySelector('button:not([disabled])'))?.focus();
    },
    close(dialog) {
        if (dialog?.open) dialog.close();
    }
};
