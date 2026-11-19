/* ---------------------------------------------------------------------------
   Hover layer for the inline SVG charts.

   The charts themselves are rendered server-side as plain SVG, and every chart
   ships a <details> table with the same numbers - so this file only adds a
   tooltip. If it never loads, no information is lost.
   --------------------------------------------------------------------------- */

(function chartTooltips() {
  const charts = document.querySelectorAll(".chart[data-tooltip]");

  charts.forEach((chart) => {
    const tip = document.createElement("div");
    tip.className = "chart-tip";
    tip.setAttribute("role", "presentation");
    chart.appendChild(tip);

    function show(mark) {
      const label = mark.dataset.tipLabel ?? "";
      const value = mark.dataset.tipValue ?? "";
      tip.innerHTML = "";

      const name = document.createElement("span");
      name.textContent = label + "  ";
      const amount = document.createElement("b");
      amount.textContent = value;
      tip.append(name, amount);

      const box = mark.getBoundingClientRect();
      const host = chart.getBoundingClientRect();
      const x = box.left - host.left + box.width / 2;
      const y = box.top - host.top - 6;

      // Keep the tooltip inside the card on the edges of the chart.
      const half = tip.offsetWidth / 2 || 60;
      tip.style.left = Math.min(Math.max(x, half + 4), host.width - half - 4) + "px";
      tip.style.top = Math.max(y, 24) + "px";
      tip.dataset.show = "true";
    }

    function hide() {
      tip.dataset.show = "false";
    }

    chart.addEventListener("pointerover", (event) => {
      const mark = event.target.closest(".chart-mark");
      if (mark) show(mark);
    });

    chart.addEventListener("pointerleave", hide);

    chart.addEventListener("focusin", (event) => {
      const mark = event.target.closest(".chart-mark");
      if (mark) show(mark);
    });

    chart.addEventListener("focusout", hide);
  });
})();
