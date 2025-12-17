document.addEventListener("DOMContentLoaded", function () {
  const totalRowsInput = document.getElementById("total-rows-input");
  const totalColumnsInput = document.getElementById("total-columns-input");
  const planGrid = document.getElementById("plan-grid");
  const ticketTypesContainer = document.getElementById(
    "ticket-types-container"
  );
  const addTypeBtn = document.getElementById("add-type-btn");
  const typeTemplate = document.getElementById("type-template").innerHTML;
  const noTypesMessage = document.getElementById("no-types-message");
  const legendContainer = document.getElementById("legend-container");
  const activeSelectionInfo = document.getElementById("active-selection-info");
  const activeTypeNameDisplay = document.getElementById("active-type-name");
  const rowLetterInfo = document.getElementById("row-letter-info");
  const colNumberInfo = document.getElementById("col-number-info");

  let typeIndex = 0;
  let activeTicketTypeIndex = -1;
  let isDragging = false;
  let initialSelectionState = null;

  function getDefaultIconSvg() {
    return `
                    <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="w-5 h-5 text-blue-500 lucide lucide-lock-keyhole-icon lucide-lock-keyhole lock-icon-container"><circle cx="12" cy="16" r="1"/><rect x="3" y="10" width="18" height="12" rx="2"/><path d="M7 10V7a5 5 0 0 1 10 0v3"/></svg>
                `;
  }

  function getActiveIconSvg() {
    return `
                    <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="w-5 h-5 text-blue-500 lucide lucide-lock-keyhole-open-icon lucide-lock-keyhole-open lock-icon-container"><circle cx="12" cy="16" r="1"/><rect width="18" height="12" x="3" y="10" rx="2"/><path d="M7 10V7a5 5 0 0 1 9.33-2.5"/></svg>
                `;
  }

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

  // Obtient l'état actuel de tous les types de tickets
  function getCurrentTicketTypePlans() {
    const typeElements =
      ticketTypesContainer.querySelectorAll(".ticket-type-row");
    const plans = [];
    typeElements.forEach((el) => {
      let seatsArray;
      const isReserved =
        el.querySelector(".is-reserved-seating").value === "true";
      const jsonValue = el.querySelector(".selected-seats-json").value || "[]";

      try {
        // Récupérer tous les sièges, qu'il s'agisse d'une sélection réservée ou d'une zone libre
        seatsArray = JSON.parse(jsonValue);
      } catch (e) {
        seatsArray = [];
        console.error("Erreur de parsing JSON pour les sièges:", e);
      }

      const plan = {
        name: el.querySelector(".type-name-input").value,
        price: parseFloat(el.querySelector(".type-price-input").value),
        color: el.querySelector(".type-color-value").value,
        isReservedSeating: isReserved,
        selectedSeats: new Set(seatsArray),
        element: el,
      };
      if (plan.name) {
        plans.push(plan);
      }
    });
    return plans;
  }

  // Détermine le Type de Ticket d'un siège donné (basé sur 'A-1')
  function getTicketTypeForSeat(seatId, plans) {
    // Le siège est assigné à un type si ce type contient l'ID du siège
    for (let i = 0; i < plans.length; i++) {
      if (plans[i].selectedSeats.has(seatId)) {
        return plans[i];
      }
    }
    return null;
  }

  // Génère et dessine la grille
  function renderGrid() {
    const totalRows = parseInt(totalRowsInput.value) || 0;
    const totalColumns = parseInt(totalColumnsInput.value) || 0;
    const plans = getCurrentTicketTypePlans();

    planGrid.innerHTML = "";
    legendContainer.innerHTML = "";

    if (totalRows <= 0 || totalColumns <= 0) {
      planGrid.style.display = "none";
      return;
    }

    planGrid.style.display = "grid";
    planGrid.style.gridTemplateColumns = `repeat(${totalColumns}, 1fr)`;
    planGrid.style.gridTemplateRows = `repeat(${totalRows}, 1fr)`;

    // Charger le layout du lieu
    const layoutJsonInput = document.getElementById("venue-layout-json");
    let venueLayout = [];
    try {
      if (layoutJsonInput && layoutJsonInput.value) {
        venueLayout = JSON.parse(layoutJsonInput.value);
      }
    } catch (e) {
      console.error("Error parsing venue layout", e);
    }

    // Mettre à jour les compteurs de sièges/zones
    plans.forEach((plan) => {
      const countSpan = plan.element.querySelector(".seat-count-display");
      if (countSpan) {
        if (plan.isReservedSeating) {
          countSpan.textContent = `${plan.selectedSeats.size} sièges assignés`;
        } else {
          countSpan.textContent = `${plan.selectedSeats.size} sièges assignés (Zone Libre)`;
        }
      }
    });

    // Dessin des sièges
    for (let r = 1; r <= totalRows; r++) {
      for (let c = 1; c <= totalColumns; c++) {
        const rowLetter = indexToLetter(r);
        const seatId = `${rowLetter}-${c}`;
        const cell = document.createElement("div");
        cell.classList.add("seat-cell");
        cell.dataset.seatId = seatId;
        cell.dataset.row = r;
        cell.dataset.col = c;
        cell.textContent = `${rowLetter}${c}`; // A1, A2, B1, ...

        // Vérifier si la cellule est vide dans la disposition du lieu
        const voidCell = venueLayout.find(
          (l) => l.r === r && l.c === c && l.type === "void"
        );

        if (voidCell) {
          cell.style.backgroundColor = "#ef4444";
          cell.style.color = "#fff";
          cell.style.cursor = "not-allowed";
          cell.setAttribute(
            "title",
            `Zone Non Assignable${voidCell.label ? ": " + voidCell.label : ""}`
          );
          cell.classList.add("void-cell");

          if (voidCell.label) {
            cell.innerHTML = `<span class="grid-cell-label">${voidCell.label}</span>`;
          }
        } else {
          const assignedType = getTicketTypeForSeat(seatId, plans);

          if (assignedType) {
            cell.style.backgroundColor = assignedType.color;
            cell.setAttribute(
              "title",
              `Rangée ${rowLetter}, Colonne ${c} - ${assignedType.name} (${
                assignedType.isReservedSeating ? "Réservé" : "Libre"
              })`
            );
            cell.style.color = getContrastColor(assignedType.color);
          } else {
            // Sièges non assignés (Hors Vente/Zone)
            cell.style.backgroundColor = "#d1d5db";
            cell.style.color = "#4b5563";
            cell.setAttribute(
              "title",
              `Rangée ${rowLetter}, Colonne ${c} - Non assigné`
            );
          }

          // La grille est toujours cliquable si un type est actif
          if (activeTicketTypeIndex === -1) {
            cell.style.cursor = "default";
          } else {
            cell.style.cursor = "pointer";
          }
        }

        planGrid.appendChild(cell);
      }
    }

    // Génération de la Légende
    const uniquePlans = {};
    plans.forEach((plan) => {
      uniquePlans[plan.name] = {
        color: plan.color,
        price: plan.price,
        isReserved: plan.isReservedSeating,
      };
    });

    Object.keys(uniquePlans).forEach((name) => {
      const planData = uniquePlans[name];
      const legendItem = document.createElement("div");
      legendItem.className = "flex items-center text-sm text-gray-700";
      legendItem.innerHTML = `
                        <span class="color-display mr-2 w-6 h-3" style="background-color: ${
                          planData.color
                        };"></span>
                        ${name} (${planData.price} Ar) (${
        planData.isReserved ? "Réservé" : "Libre"
      })
                    `;
      legendContainer.appendChild(legendItem);
    });

    // Ajouter la légende pour les places non assignées
    const defaultLegend = document.createElement("div");
    defaultLegend.className = "flex items-center text-sm text-gray-700";
    defaultLegend.innerHTML = `
                    <span class="color-display mr-2 w-6 h-3" style="background-color: #d1d5db;"></span>
                    Non Assigné (Hors Vente/Zone)
                `;
    legendContainer.appendChild(defaultLegend);

    // Ajouter la légende pour les zones vides
    const voidLegend = document.createElement("div");
    voidLegend.className = "flex items-center text-sm text-gray-700 ml-4";
    voidLegend.innerHTML = `
                    <span class="color-display mr-2 w-6 h-3" style="background-color: #ef4444;"></span>
                    Zone Non Assignable
                `;
    legendContainer.appendChild(voidLegend);
  }

  // 4. Mettre à jour les index des inputs et rafraîchir la grille
  function updateIndices() {
    const rows = ticketTypesContainer.querySelectorAll(".ticket-type-row");
    typeIndex = rows.length;

    if (noTypesMessage) {
      noTypesMessage.classList.toggle("hidden", rows.length > 0);
    }

    rows.forEach((row, index) => {
      row.setAttribute("data-index", index);

      // Mettre à jour l'index dans les noms de champs
      row.querySelectorAll("input, select").forEach((input) => {
        const originalName = input.getAttribute("name");
        if (originalName) {
          input.setAttribute(
            "name",
            originalName.replace(/\[\d+\]/, "[" + index + "]")
          );
        }
        // Mettre à jour l'index pour le color picker (si nécessaire)
        if (input.classList.contains("color-picker-input")) {
          input.dataset.index = index;
        }
      });
    });

    renderGrid();
  }

  // Gère l'assignation/désassignation d'un siège
  function toggleSeatAssignment(seatId, plans, activePlan) {
    // Déterminer l'état initial du siège par rapport au type actif
    const isAssignedToActive = activePlan.selectedSeats.has(seatId);
    let shouldAssign;

    if (isDragging) {
      // En mode glisser, utiliser l'état initial de la sélection
      shouldAssign = initialSelectionState;
    } else {
      // En mode clic simple, inverser l'état
      shouldAssign = !isAssignedToActive;
      initialSelectionState = shouldAssign; // Définir l'état pour les glissements suivants
    }

    const assignedPlan = getTicketTypeForSeat(seatId, plans);

    if (shouldAssign) {
      // Tentative d'assignation
      if (assignedPlan) {
        // Siège déjà pris par un autre type -> l'enlever de l'autre type
        assignedPlan.selectedSeats.delete(seatId);
        // Mettre à jour le JSON de l'ancien type
        const oldSeatsArray = Array.from(assignedPlan.selectedSeats);
        assignedPlan.element.querySelector(".selected-seats-json").value =
          JSON.stringify(oldSeatsArray);
      }
      // Assignation au type actif
      activePlan.selectedSeats.add(seatId);
    } else {
      // Tentative de désassignation
      if (assignedPlan === activePlan) {
        activePlan.selectedSeats.delete(seatId);
      }
    }

    // Mettre à jour la valeur JSON dans l'input caché du type actif
    const seatsArray = Array.from(activePlan.selectedSeats);
    activePlan.element.querySelector(".selected-seats-json").value =
      JSON.stringify(seatsArray);

    // Mettre à jour le style de la cellule et le compteur (partiel render)
    const cell = planGrid.querySelector(`[data-seat-id="${seatId}"]`);
    if (cell) {
      if (activePlan.selectedSeats.has(seatId)) {
        cell.style.backgroundColor = activePlan.color;
        cell.style.color = getContrastColor(activePlan.color);
      } else {
        // Le siège est maintenant libre (non assigné)
        cell.style.backgroundColor = "#d1d5db";
        cell.style.color = "#4b5563";
      }
      // Mettre à jour le compteur
      const countSpan = activePlan.element.querySelector(".seat-count-display");
      if (countSpan) {
        const modeText = activePlan.isReservedSeating
          ? "sièges assignés"
          : "sièges assignés (Zone Libre)";
        countSpan.textContent = `${activePlan.selectedSeats.size} ${modeText}`;
      }
    }
  }

  // Logique d'activation de ligne

  function setActiveSelection(index) {
    // Désactiver l'ancien type
    const currentlyActive = ticketTypesContainer.querySelector(
      ".ticket-type-row.active-selection"
    );
    if (currentlyActive) {
      currentlyActive.classList.remove("active-selection");

      const oldIconContainer = currentlyActive.querySelector(
        ".lock-icon-container"
      );
      if (oldIconContainer) {
        oldIconContainer.outerHTML = getDefaultIconSvg();
      }
    }

    if (index === activeTicketTypeIndex) {
      // Désactivation
      activeTicketTypeIndex = -1;
      activeSelectionInfo.classList.add("hidden");
    } else {
      // Activation du nouveau type
      activeTicketTypeIndex = index;
      const newActiveRow = ticketTypesContainer.querySelector(
        `.ticket-type-row[data-index="${index}"]`
      );
      if (newActiveRow) {
        newActiveRow.classList.add("active-selection");

        const newIconContainer = newActiveRow.querySelector(
          ".lock-icon-container"
        );
        if (newIconContainer) {
          newIconContainer.outerHTML = getActiveIconSvg();
        }

        // Mettre à jour l'info sur la sélection
        const isReserved =
          newActiveRow.querySelector(".is-reserved-seating").value === "true";
        const modeLabel = isReserved ? "Réservé" : "Libre";
        activeSelectionInfo.querySelector(
          ".font-semibold"
        ).textContent = `Mode sélection actif : ${
          newActiveRow.querySelector(".type-name-input").value
        } (${modeLabel})`;
        activeSelectionInfo.classList.remove("hidden");
      }
    }
    renderGrid(); // Mettre à jour les compteurs
  }

  // Événements

  // Gestion de l'ajout/suppression/activation des types
  addTypeBtn.addEventListener("click", function () {
    const newRowHtml = typeTemplate.replace(/__INDEX__/g, typeIndex);
    ticketTypesContainer.insertAdjacentHTML("beforeend", newRowHtml);
    typeIndex++;
    updateIndices();
    setActiveSelection(typeIndex - 1);
  });

  ticketTypesContainer.addEventListener("click", function (e) {
    const target = e.target;
    const targetRow = target.closest(".ticket-type-row");
    if (!targetRow) return;

    // Gère le bouton de suppression
    if (target.classList.contains("remove-type-btn")) {
      e.stopPropagation();
      targetRow.remove();
      updateIndices();
      // Si on supprime la ligne active, on désactive la sélection
      if (parseInt(targetRow.dataset.index) === activeTicketTypeIndex) {
        setActiveSelection(-1);
      }
      return;
    }

    // Gère les clics sur les éléments de formulaire interactifs
    // Si l'utilisateur clique sur un champ qu'il veut éditer, on ne change pas l'état actif.
    if (target.matches("input, select")) {
      return;
    }

    // Bascule/Définit la sélection active pour le reste de la ligne
    const index = parseInt(targetRow.dataset.index);
    setActiveSelection(index);
  });

  // Mise à jour de la Couleur/Nom/Placement (Utilisation de l'événement 'input' pour les changements)
  ticketTypesContainer.addEventListener("input", function (e) {
    const target = e.target;
    const row = target.closest(".ticket-type-row");
    const index = parseInt(row.dataset.index);

    if (target.classList.contains("color-picker-input")) {
      const color = target.value;
      row.setAttribute("data-color", color);
      row.querySelector(".type-color-value").value = color;
      renderGrid();
    } else if (target.classList.contains("type-name-input")) {
      if (index === activeTicketTypeIndex) {
        const isReserved =
          row.querySelector(".is-reserved-seating").value === "true";
        const modeLabel = isReserved ? "Réservé" : "Libre";
        activeSelectionInfo.querySelector(
          ".font-semibold"
        ).textContent = `Mode sélection actif : ${target.value} (${modeLabel})`;
      }
      renderGrid();
    } else if (target.classList.contains("is-reserved-seating")) {
      // Mettre à jour le mode de sélection si le type actif est modifié
      if (index === activeTicketTypeIndex) {
        const isReserved = target.value === "true";
        const modeLabel = isReserved ? "Réservé" : "Libre";
        activeSelectionInfo.querySelector(
          ".font-semibold"
        ).textContent = `Mode sélection actif : ${
          row.querySelector(".type-name-input").value
        } (${modeLabel})`;
      }
      renderGrid(); // Redessine pour mettre à jour la légende et les compteurs
    }
  });

  // Événements de souris pour la sélection multiple

  // MOUSE DOWN : Début de la sélection
  planGrid.addEventListener("mousedown", function (e) {
    // La grille est toujours cliquable
    if (activeTicketTypeIndex === -1) {
      // Indiquer à l'utilisateur qu'il doit sélectionner un type
      console.warn(
        "Veuillez sélectionner un Type de Ticket à assigner d'abord."
      );
      return;
    }

    isDragging = true;
    e.preventDefault();

    const cell = e.target.closest(".seat-cell");
    if (cell) {
      if (cell.classList.contains("void-cell")) return; // Ignore les void cells

      const seatId = cell.dataset.seatId;
      const plans = getCurrentTicketTypePlans();
      const activePlan = plans[activeTicketTypeIndex];

      // Déterminer si le glissement doit assigner (true) ou désassigner (false)
      // C'est l'état opposé à l'état actuel du siège cliqué PAR RAPPORT AU TYPE ACTIF.
      initialSelectionState = !activePlan.selectedSeats.has(seatId);

      // Appliquer l'action au premier clic/siège
      toggleSeatAssignment(seatId, plans, activePlan);
    }
  });

  // MOUSE MOVE : Glissement de la sélection
  planGrid.addEventListener("mousemove", function (e) {
    if (!isDragging || activeTicketTypeIndex === -1) return;

    const cell = e.target.closest(".seat-cell");
    if (cell) {
      if (cell.classList.contains("void-cell")) return; // Ignore les void cells

      const seatId = cell.dataset.seatId;
      const plans = getCurrentTicketTypePlans();
      const activePlan = plans[activeTicketTypeIndex];

      // Appliquer l'action basée sur l'état initial (initialSelectionState)
      toggleSeatAssignment(seatId, plans, activePlan);

      // Mise à jour de l'information sur le siège survolé
      if (rowLetterInfo) rowLetterInfo.textContent = indexToLetter(parseInt(cell.dataset.row));
      if (colNumberInfo) colNumberInfo.textContent = cell.dataset.col;
    }
  });

  // MOUSE UP / MOUSE LEAVE : Fin de la sélection
  document.addEventListener("mouseup", function () {
    isDragging = false;
    initialSelectionState = null;
  });
  planGrid.addEventListener("mouseleave", function () {
    // Pour éviter que le drag ne continue si la souris quitte la grille rapidement
    // isDragging = false; // Désactivé car mouseup est plus sûr
  });

  // 5. MOUSE MOVE sur la grille (sans drag) pour l'affichage des coordonnées
  planGrid.addEventListener("mousemove", function (e) {
    if (activeTicketTypeIndex === -1) return;
    const cell = e.target.closest(".seat-cell");
    if (cell) {
      if (rowLetterInfo) rowLetterInfo.textContent = indexToLetter(parseInt(cell.dataset.row));
      if (colNumberInfo) colNumberInfo.textContent = cell.dataset.col;
    }
  });

  // Initialisation au chargement
  updateIndices();
});
