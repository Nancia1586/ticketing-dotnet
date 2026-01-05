document.addEventListener("DOMContentLoaded", function () {
  const totalRowsInput = document.getElementById("total-rows-input");
  const totalColumnsInput = document.getElementById("total-columns-input");
  const planGrid = document.getElementById("plan-grid");
  const ticketTypesContainer = document.getElementById("ticket-types-container");
  const legendContainer = document.getElementById("legend-container");

  function indexToLetter(index) {
    if (index < 1 || index > 26) return index.toString();
    return String.fromCharCode(64 + index);
  }

  function getContrastColor(hexColor) {
    const r = parseInt(hexColor.substring(1, 3), 16);
    const g = parseInt(hexColor.substring(3, 5), 16);
    const b = parseInt(hexColor.substring(5, 7), 16);
    const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
    return luminance > 0.5 ? "#000000" : "#ffffff";
  }

  function getCurrentTicketTypePlans() {
    const typeElements = ticketTypesContainer.querySelectorAll(".ticket-type-row");
    const plans = [];
    typeElements.forEach((el) => {
      const jsonValue = el.querySelector(".selected-seats-json").value || "[]";
      let seatsArray = [];
      try {
        seatsArray = JSON.parse(jsonValue);
      } catch (e) {
        console.error("Erreur parsing JSON:", e);
      }

      plans.push({
        name: el.querySelector(".type-name-input").value,
        color: el.querySelector(".type-color-value").value,
        selectedSeats: new Set(seatsArray),
      });
    });
    return plans;
  }

  function getTicketTypeForSeat(seatId, plans) {
    for (const plan of plans) {
      if (plan.selectedSeats.has(seatId)) return plan;
    }
    return null;
  }

  function renderGrid() {
    const totalRows = parseInt(totalRowsInput.value) || 0;
    const totalColumns = parseInt(totalColumnsInput.value) || 0;
    const plans = getCurrentTicketTypePlans();

    planGrid.innerHTML = "";
    legendContainer.innerHTML = "";

    if (totalRows <= 0 || totalColumns <= 0) {
      planGrid.textContent = "Aucun plan défini.";
      return;
    }

    planGrid.style.display = "grid";
    planGrid.style.gridTemplateColumns = `repeat(${totalColumns}, 1fr)`;
    planGrid.style.gridTemplateRows = `repeat(${totalRows}, 1fr)`;

    const layoutJsonInput = document.getElementById("venue-layout-json");
    let venueLayout = [];
    try {
      if (layoutJsonInput && layoutJsonInput.value) {
        venueLayout = JSON.parse(layoutJsonInput.value);
      }
    } catch (e) {}

    for (let r = 1; r <= totalRows; r++) {
      for (let c = 1; c <= totalColumns; c++) {
        const rowLetter = indexToLetter(r);
        const seatId = `${rowLetter}-${c}`;
        const cell = document.createElement("div");
        cell.classList.add("seat-cell", "details-mode");
        cell.textContent = `${rowLetter}${c}`;

        const voidCell = venueLayout.find((l) => l.r === r && l.c === c && l.type === "void");

        if (voidCell) {
          cell.style.backgroundColor = "#f3f4f6";
          cell.style.color = "#d1d5db";
          cell.classList.add("void-cell");
          if (voidCell.label) {
            cell.innerHTML = `<span class="grid-cell-label text-[8px]">${voidCell.label}</span>`;
          }
        } else {
          const assignedType = getTicketTypeForSeat(seatId, plans);
          if (assignedType) {
            cell.style.backgroundColor = assignedType.color;
            cell.style.color = getContrastColor(assignedType.color);
          } else {
            cell.style.backgroundColor = "#ffffff";
            cell.style.color = "#cbd5e1";
            cell.style.border = "1px solid #f1f5f9";
          }
        }
        planGrid.appendChild(cell);
      }
    }

    // Legend
    plans.forEach((plan) => {
      const item = document.createElement("div");
      item.className = "flex items-center gap-2";
      item.innerHTML = `<span class="w-3 h-3 rounded-full" style="background-color: ${plan.color}"></span>
                        <span class="text-xs font-bold text-gray-600 truncate">${plan.name}</span>`;
      legendContainer.appendChild(item);
    });

    const unassigned = document.createElement("div");
    unassigned.className = "flex items-center gap-2";
    unassigned.innerHTML = `<span class="w-3 h-3 rounded-full bg-white border border-gray-200"></span>
                            <span class="text-xs font-bold text-gray-400 uppercase">Non assigné</span>`;
    legendContainer.appendChild(unassigned);
  }

  renderGrid();
});
