window.tripSplitInterop = {
  copyShareLink: function (token) {
    const url = `${window.location.origin}/join/${token}`;
    return navigator.clipboard.writeText(url);
  },

  downloadFile: function (filename, content, mimeType) {
    const blob = new Blob([content], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
  },

  // Delegated paste listener on the expense table body: reads the pasted
  // text directly off the native ClipboardEvent (Blazor's ClipboardEventArgs
  // doesn't expose clipboard contents) and forwards it to .NET along with
  // which cell (row index / field) the paste landed on.
  attachExpensePasteHandler: function (tbodyId, dotNetRef) {
    const tbody = document.getElementById(tbodyId);
    if (!tbody) return;
    tbody.addEventListener("paste", function (e) {
      const target = e.target;
      if (!target || !target.dataset || !target.dataset.field || !target.dataset.idx) return;
      const idx = parseInt(target.dataset.idx, 10);
      if (Number.isNaN(idx)) return;
      const text = (e.clipboardData || window.clipboardData).getData("text");
      if (!text || !/[\t\n]/.test(text)) return; // let single-value pastes behave normally
      e.preventDefault();
      dotNetRef.invokeMethodAsync("OnExpensePasteFromJs", idx, target.dataset.field, text);
    });
  },

  // Closes any open <details> popover (tag pickers, the account menu, etc.)
  // when the user opens another one or clicks elsewhere on the page, so only
  // one popover is ever open at a time. Generalized beyond .tag-popover so a
  // single call (from anywhere) covers every popover in the app - repeat
  // calls are harmless since they just attach the same delegated listeners.
  attachPopoverAutoClose: function () {
    document.addEventListener("click", function (e) {
      document.querySelectorAll(".tag-popover[open], .account-menu[open]").forEach(function (popover) {
        if (!popover.contains(e.target)) popover.removeAttribute("open");
      });
    });
    document.addEventListener("toggle", function (e) {
      if (e.target.tagName !== "DETAILS" || !e.target.open) return;
      if (!e.target.classList.contains("tag-popover") && !e.target.classList.contains("account-menu")) return;
      const selector = e.target.classList.contains("tag-popover") ? ".tag-popover[open]" : ".account-menu[open]";
      document.querySelectorAll(selector).forEach(function (popover) {
        if (popover !== e.target) popover.removeAttribute("open");
      });
    }, true);
  }
};
