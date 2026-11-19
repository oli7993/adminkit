/* ---------------------------------------------------------------------------
   Progressive enhancement only. Every feature below has a working no-JS path:
   the theme falls back to the OS setting, the sidebar is open by default,
   the filter form has a real submit button, and toasts are server-rendered.
   --------------------------------------------------------------------------- */

(function theme() {
  const KEY = "adminkit.theme";
  const root = document.documentElement;

  function store(value) {
    try {
      if (value) localStorage.setItem(KEY, value);
      else localStorage.removeItem(KEY);
    } catch {
      /* private mode, blocked site data - the toggle still works for this page */
    }
  }

  function current() {
    if (root.dataset.theme) return root.dataset.theme;
    return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
  }

  function apply(value) {
    root.dataset.theme = value;
    store(value);
    document.querySelectorAll("[data-theme-toggle]").forEach((button) => {
      button.setAttribute("aria-checked", String(value === "dark"));
      const label = button.querySelector("[data-theme-label]");
      if (label) label.textContent = value === "dark" ? "Dark" : "Light";
    });
  }

  document.addEventListener("click", (event) => {
    const button = event.target.closest("[data-theme-toggle]");
    if (!button) return;
    apply(current() === "dark" ? "light" : "dark");
  });

  apply(current());
})();

(function sidebar() {
  const KEY = "adminkit.sidebar";
  const shell = document.querySelector(".shell");
  if (!shell) return;

  const DESKTOP = window.matchMedia("(min-width: 861px)");

  try {
    if (localStorage.getItem(KEY) === "collapsed") shell.dataset.sidebar = "collapsed";
  } catch {
    /* ignore */
  }

  function closeMobile() {
    shell.removeAttribute("data-mobile-nav");
    const trigger = document.querySelector(".hamburger");
    if (trigger) {
      trigger.setAttribute("aria-expanded", "false");
      trigger.focus();
    }
  }

  document.addEventListener("click", (event) => {
    if (event.target.closest(".sidebar-scrim")) {
      closeMobile();
      return;
    }

    const trigger = event.target.closest("[data-sidebar-toggle]");
    if (!trigger) return;

    if (DESKTOP.matches) {
      const collapsed = shell.dataset.sidebar === "collapsed";
      if (collapsed) shell.removeAttribute("data-sidebar");
      else shell.dataset.sidebar = "collapsed";
      trigger.setAttribute("aria-expanded", String(collapsed));
      try {
        localStorage.setItem(KEY, collapsed ? "expanded" : "collapsed");
      } catch {
        /* ignore */
      }
      return;
    }

    const open = shell.dataset.mobileNav === "open";
    if (open) closeMobile();
    else {
      shell.dataset.mobileNav = "open";
      trigger.setAttribute("aria-expanded", "true");
      const first = document.querySelector(".sidebar .nav-item");
      if (first) first.focus();
    }
  });

  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape" && shell.dataset.mobileNav === "open") closeMobile();
  });
})();

(function filters() {
  const form = document.querySelector("[data-autosubmit]");
  if (!form) return;

  let timer;
  const submit = () => form.requestSubmit();

  form.addEventListener("input", (event) => {
    const field = event.target;
    if (field.type === "search" || field.type === "text") {
      window.clearTimeout(timer);
      timer = window.setTimeout(submit, 350);
    }
  });

  form.addEventListener("change", (event) => {
    if (event.target.tagName === "SELECT") submit();
  });

  // The explicit Apply button is for the no-JS path; it is redundant once we are here.
  form.querySelectorAll("[data-nojs-only]").forEach((element) => element.remove());
})();

(function toasts() {
  document.addEventListener("click", (event) => {
    const close = event.target.closest("[data-toast-close]");
    if (close) close.closest(".toast")?.remove();
  });

  document.querySelectorAll(".toast").forEach((toast) => {
    window.setTimeout(() => toast.remove(), 6000);
  });
})();

(function confirmDialogs() {
  document.addEventListener("click", (event) => {
    const opener = event.target.closest("[data-dialog]");
    if (opener) {
      const dialog = document.getElementById(opener.dataset.dialog);
      if (dialog && typeof dialog.showModal === "function") {
        event.preventDefault();
        dialog.showModal();
      }
      return;
    }

    const closer = event.target.closest("[data-dialog-close]");
    if (closer) closer.closest("dialog")?.close();
  });
})();

(function focusFirstError() {
  const invalid = document.querySelector(".input-error");
  if (invalid) invalid.focus({ preventScroll: false });
})();
