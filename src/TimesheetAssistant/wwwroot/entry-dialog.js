window.entryDialog = {
    open(dialog, receiver) {
        const previous = document.activeElement;
        dialog.addEventListener('cancel', event => {
            event.preventDefault();
            receiver.invokeMethodAsync('DismissAsync');
        });
        dialog.addEventListener('close', () => {
            if (previous && previous.isConnected) previous.focus();
        }, { once: true });
        dialog.showModal();
        dialog.querySelector('input:not([disabled])')?.focus();
    },
    close(dialog) {
        if (dialog?.open) dialog.close();
    }
};
