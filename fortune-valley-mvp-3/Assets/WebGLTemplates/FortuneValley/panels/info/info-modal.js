/* ============================================================
   Fortune Valley Info Modal Controller

   Mounts a single modal into the panel's document and listens
   (via event delegation on document) for clicks on any element
   with [data-info-key]. This works for static buttons in HTML
   and for buttons rendered later inside JS template strings.

   Each panel iframe loads its own copy of this script, so each
   iframe ends up with its own modal instance scoped to that
   document. There is no cross-iframe state.

   Closes via:
     - Click on the close (X) button
     - Click on the "Got it" button
     - Click on the backdrop (outside the card)
     - Escape key

   While open, focus is trapped inside the modal so Tab cycles
   between the close button and the Got it button. On close,
   focus is restored to the element that opened the modal.
   ============================================================ */

import { TERM_DEFINITIONS } from "./term-definitions.js";

const FOCUSABLE = 'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])';

let backdropEl = null;
let cardEl = null;
let titleEl = null;
let bodyEl = null;
let closeBtn = null;
let gotItBtn = null;
let lastTrigger = null;

function escapeHtml(s) {
  return String(s)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");
}

function ensureMounted() {
  if (backdropEl) return;

  backdropEl = document.createElement("div");
  backdropEl.className = "fv-info-backdrop";
  backdropEl.setAttribute("hidden", "");
  backdropEl.innerHTML = `
    <div class="fv-info-card" role="dialog" aria-modal="true" aria-labelledby="fv-info-title">
      <div class="fv-info-header">
        <h2 class="fv-info-title" id="fv-info-title">Term</h2>
        <button type="button" class="fv-info-close" aria-label="Close">&#x2715;</button>
      </div>
      <div class="fv-info-body"></div>
      <div class="fv-info-footer">
        <button type="button" class="fv-info-got-it">Got it</button>
      </div>
    </div>
  `;
  document.body.appendChild(backdropEl);

  cardEl   = backdropEl.querySelector(".fv-info-card");
  titleEl  = backdropEl.querySelector(".fv-info-title");
  bodyEl   = backdropEl.querySelector(".fv-info-body");
  closeBtn = backdropEl.querySelector(".fv-info-close");
  gotItBtn = backdropEl.querySelector(".fv-info-got-it");

  closeBtn.addEventListener("click", close);
  gotItBtn.addEventListener("click", close);

  // Click on the backdrop (but not the card itself) closes.
  backdropEl.addEventListener("click", (ev) => {
    if (ev.target === backdropEl) close();
  });

  // Escape closes; Tab cycles inside the modal.
  document.addEventListener("keydown", onKeydown);
}

function buildBody(entry) {
  const sections = [
    { title: "What is this?",      text: entry.what },
    { title: "Why it matters",     text: entry.why  },
    { title: "How it works in-game", text: entry.how },
  ].map((s) => `
    <div class="fv-info-section">
      <div class="fv-info-section-title">${escapeHtml(s.title)}</div>
      <p class="fv-info-section-body">${escapeHtml(s.text)}</p>
    </div>
  `).join("");

  let subs = "";
  if (Array.isArray(entry.subsections) && entry.subsections.length) {
    const items = entry.subsections.map((it) => `
      <li class="fv-info-subitem">
        <div class="fv-info-subname">${escapeHtml(it.name)}</div>
        <div class="fv-info-subdesc">${escapeHtml(it.desc)}</div>
      </li>
    `).join("");
    subs = `
      <div class="fv-info-section">
        <div class="fv-info-section-title">What each one means</div>
        <ul class="fv-info-sublist">${items}</ul>
      </div>
    `;
  }

  return sections + subs;
}

function open(key, trigger) {
  ensureMounted();

  const entry = TERM_DEFINITIONS[key];
  if (!entry) {
    console.warn("[FV.info] no definition for key:", key);
    return;
  }

  lastTrigger = trigger || null;

  titleEl.textContent = entry.label || key;
  bodyEl.innerHTML = buildBody(entry);
  bodyEl.scrollTop = 0;

  backdropEl.hidden = false;
  // Force reflow so the opacity transition fires.
  void backdropEl.offsetWidth;
  backdropEl.classList.add("fv-info-open");

  // Focus the close button by default. Tab will cycle to Got it.
  closeBtn.focus();
}

function close() {
  if (!backdropEl || backdropEl.hidden) return;
  backdropEl.classList.remove("fv-info-open");
  // Wait for the fade-out transition before fully hiding.
  setTimeout(() => {
    if (backdropEl) backdropEl.hidden = true;
  }, 180);

  if (lastTrigger && typeof lastTrigger.focus === "function") {
    try { lastTrigger.focus(); } catch (e) { /* element may have been removed */ }
  }
  lastTrigger = null;
}

function onKeydown(ev) {
  if (!backdropEl || backdropEl.hidden) return;

  if (ev.key === "Escape") {
    ev.preventDefault();
    close();
    return;
  }

  if (ev.key === "Tab") {
    const focusables = Array.from(cardEl.querySelectorAll(FOCUSABLE))
      .filter(el => !el.disabled && el.offsetParent !== null);
    if (!focusables.length) return;
    const first = focusables[0];
    const last  = focusables[focusables.length - 1];
    const active = document.activeElement;
    if (ev.shiftKey && active === first) {
      ev.preventDefault();
      last.focus();
    } else if (!ev.shiftKey && active === last) {
      ev.preventDefault();
      first.focus();
    }
  }
}

// Delegated click handler. Catches every [data-info-key] click,
// including buttons that were rendered into JS template strings
// after this script ran.
document.addEventListener("click", (ev) => {
  const trigger = ev.target.closest("[data-info-key]");
  if (!trigger) return;
  ev.preventDefault();
  ev.stopPropagation();
  open(trigger.getAttribute("data-info-key"), trigger);
});

// Mount eagerly on DOM ready so the first click is instant.
if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", ensureMounted);
} else {
  ensureMounted();
}
