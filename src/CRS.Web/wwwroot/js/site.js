const menuToggle = document.querySelector('.menu-toggle');
menuToggle?.addEventListener('click', () => {
    const menu = document.getElementById('navigation-links');
    const opened = menu.classList.toggle('is-open');
    menuToggle.setAttribute('aria-expanded', String(opened));
});

for (const ratingModalElement of document.querySelectorAll('[data-workflow-modal]')) {
    const modal = bootstrap.Modal.getOrCreateInstance(ratingModalElement);
    const content = ratingModalElement.querySelector('[data-workflow-content]');
    const isAdmin = ratingModalElement.dataset.workflowKind === 'admin';
    let loadController;
    let selectedCard;
    let submitting = false;
    document.addEventListener('click', async event => {
        const card = event.target.closest('a[data-workflow-open]');
        if (!card || card.dataset.workflowOpen !== ratingModalElement.id || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
        event.preventDefault();
        selectedCard = card;
        loadController?.abort();
        loadController = new AbortController();
        content.textContent = 'Loading call details…';
        modal.show();
        try {
            const response = await fetch(card.href, { headers: { 'X-Rating-Modal': 'true' }, signal: loadController.signal });
            if (!response.ok || response.redirected) throw new Error('Call details unavailable');
            content.innerHTML = await response.text();
            content.querySelector('input[type="checkbox"], select')?.focus();
        } catch (error) {
            if (error.name !== 'AbortError') content.textContent = 'Call details could not be loaded. Close this popup and try again.';
        }
    });
    ratingModalElement.addEventListener('hidden.bs.modal', () => selectedCard?.focus());
    ratingModalElement.addEventListener('hide.bs.modal', event => {
        // A submitted save must finish before the popup can be dismissed.
        if (submitting) event.preventDefault();
        else loadController?.abort();
    });
    ratingModalElement.addEventListener('submit', async event => {
        const form = event.target.closest('form[data-rating-form], form[data-admin-form]');
        if (!form) return;
        event.preventDefault();
        if (submitting) return;
        submitting = true;
        const button = form.querySelector('button:not([type="button"])');
        button.disabled = true;
        form.setAttribute('aria-busy', 'true');
        try {
            const response = await fetch(form.action, { method: 'POST', body: new FormData(form), headers: { 'X-Rating-Modal': 'true' } });
            if (response.status === 422) {
                content.innerHTML = await response.text();
                content.querySelector('[role="alert"]')?.scrollIntoView({ block: 'nearest' });
                return;
            }
            if (!response.ok || response.redirected) throw new Error('Rating submission unavailable');
            await response.json();
            // The controller has set the success/warning flash, including email-failure outcomes.
            window.location.reload();
        } catch {
            let alert = form.querySelector('.submission-error');
            if (!alert) {
                alert = document.createElement('p');
                alert.className = 'submission-error text-danger';
                alert.setAttribute('role', 'alert');
                form.prepend(alert);
            }
            alert.textContent = isAdmin ? 'The update could not be confirmed. Check the current status before trying again.' : 'The save could not be confirmed. Try again; duplicate protection prevents a second rating. If it was already saved, contact IT to check email delivery.';
        } finally {
            submitting = false;
            button.disabled = false;
            form.removeAttribute('aria-busy');
        }
    });
}
